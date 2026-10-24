import { Hono } from 'hono';
import { createPool, listWallpapers, getWallpaper, listCategories, searchWallpapers, type DatabaseBindings } from './db.js';

// Cloudflare Workers API, spec section 11.
// Real PostgreSQL catalog queries are used when a database is
// configured (DATABASE_URL / Hyperdrive). Without a database, the
// catalog is honestly empty and /v1/config reports catalogEnabled:
// false — no fake content is ever returned.
// Premium downloads: public assets return the CDN URL; premium
// assets without a configured auth/entitlement system return 501
// rather than a signed URL that bypasses entitlement checks.

export type Bindings = DatabaseBindings & {
  PUBLIC_CDN_BASE_URL?: string;
};

const app = new Hono<{ Bindings: Bindings }>();

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

app.get('/v1/health', (c) =>
  c.json({ status: 'ok', apiVersion: 'v1', build: '0.2.0' }),
);

app.get('/v1/config', (c) =>
  c.json({
    catalogEnabled: Boolean(c.env.HYPERDRIVE?.connectionString ?? c.env.DATABASE_URL),
    minClientVersion: '1.0.0',
    supportedMedia: ['video/mp4:h264', 'image/png', 'image/jpeg'],
  }),
);

app.get('/v1/wallpapers', async (c) => {
  const limit = parseLimit(c.req.query('limit'));
  if (limit === null) return c.json({ error: { code: 'invalid_request', message: 'limit must be a positive integer.' } }, 400);
  const db = createPool(c.env);
  const items = (await listWallpapers(db, limit, c.req.query('category'))) ?? [];
  return c.json({ items, nextCursor: null, limit, catalogVersion: null });
});

app.get('/v1/wallpapers/:id', async (c) => {
  const db = createPool(c.env);
  const item = await getWallpaper(db, c.req.param('id'));
  if (!item) {
    return c.json({ error: { code: 'not_found', message: 'Wallpaper not found or catalog not configured.' } }, 404);
  }
  const variants = await db?.query(
    `SELECT id, variant_key, mime_type, codec, width, height, fps, size_bytes, sha256
     FROM media_variants WHERE wallpaper_id = $1 AND processing_state = 'ready'
     ORDER BY width DESC, height DESC`,
    [item.id],
  );
  return c.json({ item, variants: variants?.rows ?? [] });
});

app.get('/v1/categories', async (c) => {
  const db = createPool(c.env);
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
  const db = createPool(c.env);
  const items = (await searchWallpapers(db, q, limit)) ?? [];
  return c.json({ items, nextCursor: null });
});

app.post('/v1/assets/:variantId/authorize-download', async (c) => {
  const db = createPool(c.env);
  if (!db) {
    return c.json({ error: { code: 'not_configured', message: 'Database not configured.' } }, 501);
  }
  const variant = await db.query(
    `SELECT mv.id, mv.object_key, w.premium, w.visibility, mv.processing_state
     FROM media_variants mv JOIN wallpapers w ON w.id = mv.wallpaper_id
     WHERE mv.id = $1`,
    [c.req.param('variantId')],
  );
  const row = variant.rows[0];
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
  const base = c.env.PUBLIC_CDN_BASE_URL ?? '';
  const downloadUrl = base ? `${base.replace(/\/$/, '')}/${row.object_key}` : row.object_key;
  return c.json({ downloadUrl, method: 'cdn' });
});

app.get('/v1/me/favorites', (c) =>
  c.json({ error: { code: 'not_implemented', message: 'Account favourites require M4 auth.' } }, 501),
);

export default app;
