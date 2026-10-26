import { Hono } from 'hono';
import { adminRoutes } from './admin.js';
import { createDb, listWallpapers, getWallpaper, listCategories, searchWallpapers, type DatabaseBindings } from './db.js';

// Cloudflare Workers API, spec section 11.
// Real D1 catalog queries are used when the DB binding is configured.
// Without a database, the catalog is honestly empty and /v1/config
// reports catalogEnabled: false — no fake content is ever returned.
// Premium downloads: public assets return the CDN URL; premium
// assets without a configured auth/entitlement system return 501
// rather than a signed URL that bypasses entitlement checks.

export type Bindings = DatabaseBindings & {
  PUBLIC_CDN_BASE_URL?: string;
  R2_PUBLISHED?: R2Bucket;
};

const app = new Hono<{ Bindings: Bindings }>();

app.route('/', adminRoutes());

// This public, stable URL is used by the Microsoft Store listing. Keep this
// statement aligned with the desktop client's actual data handling.
const privacyPolicy = `<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>Loopdesk Privacy Policy</title></head>
<body><main><h1>Loopdesk Privacy Policy</h1><p>Effective date: October 26, 2026</p>
<p>Loopdesk is a Windows desktop application for browsing, downloading, and applying live wallpapers.</p>
<h2>Information we handle</h2><p>Loopdesk does not require an account and does not collect names, email addresses, payment information, advertising identifiers, or location data. The app does not scan or upload files from your drives. It requests the catalog and wallpaper files you choose from the Loopdesk service; normal network information needed to deliver those requests, such as IP address and request metadata, may be processed by the hosting provider.</p>
<h2>Local storage</h2><p>Wallpaper files you download, their previews, and app settings are stored locally in the Loopdesk application data folder on your device. They are used to provide offline playback and can be removed through the app or by uninstalling it.</p>
<h2>Sharing</h2><p>Loopdesk does not sell or share personal information for advertising. We use Cloudflare to host the catalog and deliver selected wallpaper files.</p>
<h2>Changes</h2><p>If this policy changes, the effective date and this page will be updated.</p></main></body></html>`;

app.get('/privacy', () => new Response(privacyPolicy, {
  headers: {
    'content-type': 'text/html; charset=utf-8',
    'cache-control': 'public, max-age=3600',
    'x-content-type-options': 'nosniff',
  },
}));

app.onError((error, c) => {
  console.error('Unhandled API error', error);
  return c.json({ error: { code: 'internal_error', message: 'Request could not be completed.' } }, 500);
});

function parseLimit(value: string | undefined): number | null {
  if (value === undefined) return 24;
  if (!/^[1-9]\d*$/.test(value)) return null;
  const limit = Number(value);
  return limit <= 100 ? limit : 100;
}

function parseRange(value: string, size: number): { offset: number; length: number } | null {
  if (!Number.isSafeInteger(size) || size <= 0) return null;
  const match = /^bytes=(\d*)-(\d*)$/.exec(value);
  if (!match) return null;
  const [, startText, endText] = match;
  if (!startText && !endText) return null;
  if (!startText) {
    const length = Number(endText);
    return Number.isSafeInteger(length) && length > 0 ? { offset: Math.max(0, size - length), length: Math.min(size, length) } : null;
  }
  const offset = Number(startText);
  const end = endText ? Number(endText) : size - 1;
  if (!Number.isSafeInteger(offset) || !Number.isSafeInteger(end) || offset < 0 || end < offset || offset >= size) return null;
  return { offset, length: Math.min(end, size - 1) - offset + 1 };
}

app.get('/v1/health', (c) =>
  c.json({ status: 'ok', apiVersion: 'v1', build: '0.2.0' }),
);

app.get('/v1/config', (c) =>
  c.json({
    catalogEnabled: Boolean(c.env.DB),
    minClientVersion: '1.0.0',
    supportedMedia: ['video/mp4:h264', 'image/png', 'image/jpeg'],
  }),
);

app.get('/v1/wallpapers', async (c) => {
  const limit = parseLimit(c.req.query('limit'));
  if (limit === null) return c.json({ error: { code: 'invalid_request', message: 'limit must be a positive integer.' } }, 400);
  const db = createDb(c.env);
  const items = (await listWallpapers(db, limit, c.req.query('category'))) ?? [];
  return c.json({ items, nextCursor: null, limit, catalogVersion: null });
});

app.get('/v1/wallpapers/:id', async (c) => {
  const db = createDb(c.env);
  const item = await getWallpaper(db, c.req.param('id'));
  if (!item) {
    return c.json({ error: { code: 'not_found', message: 'Wallpaper not found or catalog not configured.' } }, 404);
  }
  const variants = await db?.prepare(
    `SELECT id, variant_key, mime_type, codec, width, height, fps, size_bytes, sha256
     FROM media_variants WHERE wallpaper_id = ? AND processing_state = 'ready'
     ORDER BY width DESC, height DESC`,
  ).bind(item.id).all();
  return c.json({ item, variants: variants?.results ?? [] });
});

app.get('/v1/categories', async (c) => {
  const db = createDb(c.env);
  const items = (await listCategories(db)) ?? [];
  return c.json({ items });
});

app.get('/v1/search', async (c) => {
  const q = c.req.query('q') ?? '';
  if (q.length > 200) {
    return c.json({ error: { code: 'invalid_request', message: 'Query too long.' } }, 400);
  }
  const limit = parseLimit(c.req.query('limit'));
  if (limit === null) return c.json({ error: { code: 'invalid_request', message: 'limit must be a positive integer.' } }, 400);
  const db = createDb(c.env);
  const items = (await searchWallpapers(db, q, limit)) ?? [];
  return c.json({ items, nextCursor: null });
});

app.post('/v1/assets/:variantId/authorize-download', async (c) => {
  const db = createDb(c.env);
  if (!db) {
    return c.json({ error: { code: 'not_configured', message: 'Database not configured.' } }, 501);
  }
  const row = await db.prepare(
    `SELECT mv.id, mv.object_key, w.premium, w.visibility, mv.processing_state
     FROM media_variants mv JOIN wallpapers w ON w.id = mv.wallpaper_id
     WHERE mv.id = ?`,
  ).bind(c.req.param('variantId')).first<{ id: string; object_key: string; premium: number; visibility: string; processing_state: string }>();
  if (!row || row.visibility !== 'published' || row.processing_state !== 'ready') {
    return c.json({ error: { code: 'not_found', message: 'Variant not found.' } }, 404);
  }
  if (row.premium) {
    // Premium: do NOT return a URL without entitlement verification.
    // Entitlement verification requires the configured OIDC + purchase
    // system; until then, premium downloads are refused, not bypassed.
    return c.json(
      { error: { code: 'not_implemented', message: 'Premium entitlement verification is not configured.' } },
      501,
    );
  }
  const base = c.env.PUBLIC_CDN_BASE_URL?.replace(/\/$/, '') ?? new URL(c.req.url).origin;
  return c.json({ downloadUrl: `${base}/v1/assets/${row.id}/content`, method: 'public' });
});

app.get('/v1/assets/:variantId/content', async (c) => {
  const db = createDb(c.env);
  if (!db || !c.env.R2_PUBLISHED) return c.json({ error: { code: 'not_configured', message: 'Asset delivery is not configured.' } }, 501);
  const row = await db.prepare(
    `SELECT mv.object_key, mv.mime_type, w.premium, w.visibility, mv.processing_state
     FROM media_variants mv JOIN wallpapers w ON w.id = mv.wallpaper_id WHERE mv.id = ?`,
  ).bind(c.req.param('variantId')).first<{ object_key: string; mime_type: string; premium: number; visibility: string; processing_state: string }>();
  if (!row || row.visibility !== 'published' || row.processing_state !== 'ready') return c.json({ error: { code: 'not_found', message: 'Variant not found.' } }, 404);
  if (row.premium) return c.json({ error: { code: 'forbidden', message: 'Premium entitlement verification is not configured.' } }, 403);
  const requestedRange = c.req.header('range');
  const head = requestedRange ? await c.env.R2_PUBLISHED.head(row.object_key) : null;
  if (requestedRange && !head) return c.json({ error: { code: 'not_found', message: 'Published object not found.' } }, 404);
  const range = requestedRange && head ? parseRange(requestedRange, head.size) : undefined;
  if (requestedRange && !range) return new Response(null, { status: 416, headers: { 'content-range': `bytes */${head!.size}`, 'accept-ranges': 'bytes' } });
  const object = await c.env.R2_PUBLISHED.get(row.object_key, range ? { range } : undefined);
  if (!object) return c.json({ error: { code: 'not_found', message: 'Published object not found.' } }, 404);
  const headers = new Headers({ 'content-type': row.mime_type, 'cache-control': 'public, max-age=31536000, immutable', etag: object.httpEtag, 'accept-ranges': 'bytes' });
  object.writeHttpMetadata(headers);
  if (range) {
    headers.set('content-range', `bytes ${range.offset}-${range.offset + range.length - 1}/${head!.size}`);
    headers.set('content-length', String(range.length));
  }
  return new Response(object.body, { status: range ? 206 : 200, headers });
});

app.get('/v1/me/favorites', (c) =>
  c.json({ error: { code: 'not_implemented', message: 'Account favourites require M4 auth.' } }, 501),
);

export default app;
