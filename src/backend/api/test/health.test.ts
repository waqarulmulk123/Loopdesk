import assert from 'node:assert/strict';
import test from 'node:test';
import app from '../src/index.js';

const env = {};

function d1ForWallpaperDetail() {
  const prepared: { sql: string; values: unknown[] }[] = [];
  const db = {
    prepare(sql: string) {
      const entry = { sql, values: [] as unknown[] };
      prepared.push(entry);
      return {
        bind(...values: unknown[]) {
          entry.values = values;
          return this;
        },
        async first() {
          if (sql.includes('FROM wallpapers')) {
            return { id: 'aurora', title: 'Aurora', description: '', kind: 'video', visibility: 'published', premium: 0, created_at: '2026-01-01' };
          }
          return null;
        },
        async all() {
          return { results: [] };
        },
      };
    },
  } as unknown as D1Database;
  return { db, prepared };
}

test('health endpoint returns the public API contract', async () => {
  const response = await app.fetch(new Request('https://api.test/v1/health'), env);
  assert.equal(response.status, 200);
  assert.deepEqual(await response.json(), { status: 'ok', apiVersion: 'v1', build: '0.2.0' });
});

test('catalog is empty without a database and rejects invalid limits', async () => {
  const catalog = await app.fetch(new Request('https://api.test/v1/wallpapers'), env);
  assert.equal(catalog.status, 200);
  assert.deepEqual(await catalog.json(), { items: [], nextCursor: null, limit: 24, catalogVersion: null });

  const invalid = await app.fetch(new Request('https://api.test/v1/wallpapers?limit=0'), env);
  assert.equal(invalid.status, 400);
});

test('wallpaper detail uses D1-compatible placeholders for variant lookup', async () => {
  const { db, prepared } = d1ForWallpaperDetail();
  const response = await app.fetch(new Request('https://api.test/v1/wallpapers/aurora'), { DB: db });
  assert.equal(response.status, 200);
  const variants = prepared.find(({ sql }) => sql.includes('FROM media_variants'));
  assert.ok(variants);
  assert.ok(variants.sql.includes('wallpaper_id = ?'));
  assert.deepEqual(variants.values, ['aurora']);
});

test('asset delivery reports missing R2 binding instead of returning an object key', async () => {
  const response = await app.fetch(new Request('https://api.test/v1/assets/variant-1/content'), { DB: {} as D1Database });
  assert.equal(response.status, 501);
  assert.deepEqual(await response.json(), { error: { code: 'not_configured', message: 'Asset delivery is not configured.' } });
});

test('asset delivery honors a single byte range for resumable downloads', async () => {
  const db = {
    prepare() {
      return {
        bind() { return this; },
        async first() { return { object_key: 'published/test.mp4', mime_type: 'video/mp4', premium: 0, visibility: 'published', processing_state: 'ready' }; },
      };
    },
  } as unknown as D1Database;
  const body = new TextEncoder().encode('2345');
  const bucket = {
    async head() { return { size: 10 }; },
    async get(_key: string, options?: R2GetOptions) {
      assert.deepEqual(options?.range, { offset: 2, length: 4 });
      return { body: new ReadableStream({ start(controller) { controller.enqueue(body); controller.close(); } }), httpEtag: 'test-etag', writeHttpMetadata() {} };
    },
  } as unknown as R2Bucket;
  const response = await app.fetch(new Request('https://api.test/v1/assets/variant-1/content', { headers: { range: 'bytes=2-5' } }), { DB: db, R2_PUBLISHED: bucket });
  assert.equal(response.status, 206);
  assert.equal(response.headers.get('content-range'), 'bytes 2-5/10');
  assert.equal(response.headers.get('accept-ranges'), 'bytes');
  assert.equal(await response.text(), '2345');
});

test('asset delivery rejects malformed byte ranges without fetching media', async () => {
  const db = {
    prepare() { return { bind() { return this; }, async first() { return { object_key: 'published/test.mp4', mime_type: 'video/mp4', premium: 0, visibility: 'published', processing_state: 'ready' }; } }; },
  } as unknown as D1Database;
  const bucket = {
    async head() { return { size: 10 }; },
    async get() { throw new Error('media must not be fetched for an invalid range'); },
  } as unknown as R2Bucket;
  const response = await app.fetch(new Request('https://api.test/v1/assets/variant-1/content', { headers: { range: 'bytes=99-100' } }), { DB: db, R2_PUBLISHED: bucket });
  assert.equal(response.status, 416);
  assert.equal(response.headers.get('content-range'), 'bytes */10');
});

test('asset delivery rejects ranges for an empty object', async () => {
  const db = {
    prepare() { return { bind() { return this; }, async first() { return { object_key: 'published/empty.mp4', mime_type: 'video/mp4', premium: 0, visibility: 'published', processing_state: 'ready' }; } }; },
  } as unknown as D1Database;
  const bucket = {
    async head() { return { size: 0 }; },
    async get() { throw new Error('empty ranged object must not be fetched'); },
  } as unknown as R2Bucket;
  const response = await app.fetch(new Request('https://api.test/v1/assets/variant-1/content', { headers: { range: 'bytes=0-' } }), { DB: db, R2_PUBLISHED: bucket });
  assert.equal(response.status, 416);
  assert.equal(response.headers.get('content-range'), 'bytes */0');
});

test('admin initialization gives each uploaded variant a unique key', async () => {
  const variants: unknown[][] = [];
  const db = {
    prepare(sql: string) {
      return {
        bind(...values: unknown[]) {
          if (sql.includes('INSERT INTO media_variants')) variants.push(values);
          return this;
        },
      };
    },
    async batch() { return []; },
  } as unknown as D1Database;
  const request = () => new Request('https://api.test/v1/admin/uploads/init', {
    method: 'POST',
    headers: { authorization: 'Bearer test-token', 'content-type': 'application/json' },
    body: JSON.stringify({ title: 'Test video', contentType: 'video/mp4', width: 1920, height: 1080, fps: 30, sizeBytes: 1024, sha256: 'a'.repeat(64) }),
  });
  const env = { DB: db, R2_PUBLISHED: {} as R2Bucket, ADMIN_API_TOKEN: 'test-token' };
  assert.equal((await app.fetch(request(), env)).status, 201);
  assert.equal((await app.fetch(request(), env)).status, 201);
  assert.equal(variants.length, 2);
  assert.match(variants[0][2] as string, /^source-/);
  assert.notEqual(variants[0][2], variants[1][2]);
});

test('admin routes are mounted and reject requests without a configured token', async () => {
  const response = await app.fetch(new Request('https://api.test/v1/admin/uploads/init', { method: 'POST' }), { DB: {} as D1Database });
  assert.equal(response.status, 503);
  assert.deepEqual(await response.json(), { error: { code: 'not_configured', message: 'Admin authentication is not configured.' } });
});

test('admin upload rejects media whose SHA-256 does not match initialization', async () => {
  let putCalled = false;
  const db = {
    prepare(sql: string) {
      return {
        bind() { return this; },
        async first() {
          if (sql.includes('FROM ingest_jobs')) return { id: 'job-1', wallpaper_id: 'wallpaper-1', state: 'awaiting_upload' };
          if (sql.includes('FROM media_variants')) return { id: 'variant-1', object_key: 'published/test.mp4', size_bytes: 12, sha256: 'a'.repeat(64) };
          return null;
        },
      };
    },
    async batch() { return []; },
  } as unknown as D1Database;
  const bucket = { async put() { putCalled = true; } } as unknown as R2Bucket;
  const body = new Uint8Array([0, 0, 0, 0, 0x66, 0x74, 0x79, 0x70, 0, 0, 0, 0]);
  const response = await app.fetch(new Request('https://api.test/v1/admin/uploads/job-1/content', {
    method: 'PUT',
    headers: { authorization: 'Bearer test-token', 'content-type': 'video/mp4', 'content-length': '12' },
    body,
  }), { DB: db, R2_PUBLISHED: bucket, ADMIN_API_TOKEN: 'test-token' });
  assert.equal(response.status, 400);
  assert.equal(putCalled, false);
});
