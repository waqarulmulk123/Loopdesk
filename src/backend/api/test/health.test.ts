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

test('admin routes are mounted and reject requests without a configured token', async () => {
  const response = await app.fetch(new Request('https://api.test/v1/admin/uploads/init', { method: 'POST' }), { DB: {} as D1Database });
  assert.equal(response.status, 503);
  assert.deepEqual(await response.json(), { error: { code: 'not_configured', message: 'Admin authentication is not configured.' } });
});
