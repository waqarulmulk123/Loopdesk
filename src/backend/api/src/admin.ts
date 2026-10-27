import { Hono } from 'hono';
import { createDb, listAdminWallpapers, type DatabaseBindings } from './db.js';

type AdminBindings = DatabaseBindings & { ADMIN_API_TOKEN?: string; R2_PUBLISHED?: R2Bucket };

type UploadRequest = {
  title?: unknown;
  description?: unknown;
  premium?: unknown;
  contentType?: unknown;
  width?: unknown;
  height?: unknown;
  fps?: unknown;
  sizeBytes?: unknown;
  sha256?: unknown;
};

type WallpaperUpdateRequest = {
  title?: unknown;
  description?: unknown;
  featured?: unknown;
  thumbnailKey?: unknown;
};
// Direct uploads are buffered for MP4 signature and SHA-256 validation.
// Keep the limit safely below a Worker memory ceiling until uploads stream to R2.
const maxUploadBytes = 40 * 1024 * 1024;
const maxThumbnailBytes = 2 * 1024 * 1024;

function positiveInteger(value: unknown, maximum: number): number | null {
  return typeof value === 'number' && Number.isInteger(value) && value > 0 && value <= maximum ? value : null;
}

function validSha256(value: unknown): string | null {
  return typeof value === 'string' && /^[a-f0-9]{64}$/i.test(value) ? value.toLowerCase() : null;
}

function validThumbnailKey(value: unknown): string | null | undefined {
  if (value === undefined) return undefined;
  if (value === null) return null;
  if (typeof value !== 'string') return undefined;
  const key = value.trim();
  return /^thumbnails\/[a-z0-9][a-z0-9._/-]{0,255}\.(?:png|jpe?g|webp)$/i.test(key) && !key.includes('..') ? key : undefined;
}

function auditStatement(db: D1Database, action: string, targetType: string, targetId: string, detail: Record<string, unknown>) {
  return db.prepare('INSERT INTO admin_audit_log (id, action, target_type, target_id, detail_json) VALUES (?, ?, ?, ?, ?)')
    .bind(crypto.randomUUID(), action, targetType, targetId, JSON.stringify(detail));
}

async function writeAudit(db: D1Database, action: string, targetType: string, targetId: string, detail: Record<string, unknown>) {
  await auditStatement(db, action, targetType, targetId, detail).run();
}

function adminLimit(value: string | undefined): number | null {
  if (value === undefined) return 50;
  if (!/^[1-9]\d*$/.test(value)) return null;
  const limit = Number(value);
  return limit <= 100 ? limit : 100;
}

function imageExtension(contentType: string): 'png' | 'jpg' | 'webp' | null {
  if (contentType === 'image/png') return 'png';
  if (contentType === 'image/jpeg') return 'jpg';
  if (contentType === 'image/webp') return 'webp';
  return null;
}

function isImage(bytes: Uint8Array, extension: 'png' | 'jpg' | 'webp'): boolean {
  if (extension === 'png') return bytes.length >= 8 && bytes[0] === 0x89 && bytes[1] === 0x50 && bytes[2] === 0x4e && bytes[3] === 0x47;
  if (extension === 'jpg') return bytes.length >= 3 && bytes[0] === 0xff && bytes[1] === 0xd8 && bytes[2] === 0xff;
  return bytes.length >= 12 && new TextDecoder().decode(bytes.slice(0, 4)) === 'RIFF' && new TextDecoder().decode(bytes.slice(8, 12)) === 'WEBP';
}

async function hasValidToken(request: Request, expected: string): Promise<boolean> {
  const supplied = request.headers.get('authorization')?.replace(/^Bearer\s+/i, '') ?? '';
  const [left, right] = await Promise.all([crypto.subtle.digest('SHA-256', new TextEncoder().encode(supplied)), crypto.subtle.digest('SHA-256', new TextEncoder().encode(expected))]);
  const a = new Uint8Array(left);
  const b = new Uint8Array(right);
  let difference = a.length ^ b.length;
  for (let i = 0; i < Math.max(a.length, b.length); i++) difference |= (a[i] ?? 0) ^ (b[i] ?? 0);
  return difference === 0;
}

export function adminRoutes(): Hono<{ Bindings: AdminBindings }> {
  const app = new Hono<{ Bindings: AdminBindings }>();

  app.use('/v1/admin/*', async (c, next) => {
    if (!c.env.ADMIN_API_TOKEN) return c.json({ error: { code: 'not_configured', message: 'Admin authentication is not configured.' } }, 503);
    if (!await hasValidToken(c.req.raw, c.env.ADMIN_API_TOKEN)) return c.json({ error: { code: 'unauthorized', message: 'Admin token required.' } }, 401);
    await next();
  });

  app.post('/v1/admin/uploads/init', async (c) => {
    const db = createDb(c.env);
    if (!db || !c.env.R2_PUBLISHED) return c.json({ error: { code: 'not_configured', message: 'Database and published R2 bindings are required.' } }, 501);
    const body = await c.req.json<UploadRequest>().catch(() => null);
    const title = typeof body?.title === 'string' ? body.title.trim() : '';
    const contentType = typeof body?.contentType === 'string' ? body.contentType : '';
    const width = positiveInteger(body?.width, 16384);
    const height = positiveInteger(body?.height, 16384);
    const fps = positiveInteger(body?.fps, 240);
    const sizeBytes = positiveInteger(body?.sizeBytes, maxUploadBytes);
    const sha256 = validSha256(body?.sha256);
    if (!title || title.length > 200 || contentType !== 'video/mp4' || !width || !height || !fps || !sizeBytes || !sha256 || body?.premium === true) {
      return c.json({ error: { code: 'invalid_request', message: 'A non-premium title, H.264 MP4 metadata (width, height, fps, sizeBytes), and sha256 are required.' } }, 400);
    }
    const wallpaperId = crypto.randomUUID();
    const variantId = crypto.randomUUID();
    const jobId = crypto.randomUUID();
    const targetKey = `published/${wallpaperId}/source.mp4`;
    await db.batch([
      db.prepare('INSERT INTO wallpapers (id, title, description, kind, visibility, premium) VALUES (?, ?, ?, ?, ?, ?)').bind(wallpaperId, title, typeof body?.description === 'string' ? body.description.slice(0, 4000) : '', 'video', 'draft', 0),
      db.prepare('INSERT INTO media_variants (id, wallpaper_id, variant_key, object_key, mime_type, codec, width, height, fps, size_bytes, sha256, processing_state) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)').bind(variantId, wallpaperId, `source-${variantId}`, targetKey, 'video/mp4', 'h264', width, height, fps, sizeBytes, sha256, 'awaiting_upload'),
      db.prepare('INSERT INTO ingest_jobs (id, wallpaper_id, state) VALUES (?, ?, ?)').bind(jobId, wallpaperId, 'awaiting_upload'),
      auditStatement(db, 'wallpaper.upload_initialized', 'wallpaper', wallpaperId, { jobId, variantId }),
    ]);
    return c.json({ jobId, wallpaperId, variantId, uploadUrl: `${new URL(c.req.url).origin}/v1/admin/uploads/${jobId}/content`, processing: 'direct_publish' }, 201);
  });

  app.put('/v1/admin/uploads/:jobId/content', async (c) => {
    const db = createDb(c.env);
    if (!db || !c.env.R2_PUBLISHED) return c.json({ error: { code: 'not_configured', message: 'Database and published R2 bindings are required.' } }, 501);
    if (c.req.header('content-type')?.split(';')[0] !== 'video/mp4' || !c.req.raw.body) return c.json({ error: { code: 'invalid_request', message: 'A video/mp4 request body is required.' } }, 400);
    const job = await db.prepare('SELECT id, wallpaper_id, state FROM ingest_jobs WHERE id = ?').bind(c.req.param('jobId')).first<{ id: string; wallpaper_id: string; state: string }>();
    if (!job) return c.json({ error: { code: 'not_found', message: 'Upload job not found.' } }, 404);
    if (job.state !== 'awaiting_upload') return c.json({ error: { code: 'invalid_state', message: 'Upload content was already received.' } }, 409);
    const variant = await db.prepare('SELECT id, object_key, size_bytes, sha256 FROM media_variants WHERE wallpaper_id = ?').bind(job.wallpaper_id).first<{ id: string; object_key: string; size_bytes: number; sha256: string }>();
    if (!variant) return c.json({ error: { code: 'invalid_state', message: 'Upload variant not found.' } }, 409);
    if (variant.size_bytes > maxUploadBytes) return c.json({ error: { code: 'payload_too_large', message: 'Direct MP4 uploads are limited to 40 MiB.' } }, 413);
    const declaredLength = c.req.header('content-length');
    if (!declaredLength || !/^\d+$/.test(declaredLength) || Number(declaredLength) !== variant.size_bytes) return c.json({ error: { code: 'invalid_request', message: 'content-length must match the initialized sizeBytes.' } }, 400);
    const bytes = new Uint8Array(await c.req.raw.arrayBuffer());
    const isMp4 = bytes.length >= 12 && new TextDecoder().decode(bytes.slice(4, 8)) === 'ftyp';
    const digest = new Uint8Array(await crypto.subtle.digest('SHA-256', bytes));
    const actualSha256 = [...digest].map(value => value.toString(16).padStart(2, '0')).join('');
    if (bytes.byteLength !== variant.size_bytes || !isMp4 || actualSha256 !== variant.sha256) {
      return c.json({ error: { code: 'invalid_upload', message: 'Upload must be the initialized MP4 with the expected size and SHA-256.' } }, 400);
    }
    await c.env.R2_PUBLISHED.put(variant.object_key, bytes, { httpMetadata: { contentType: 'video/mp4' } });
    await db.batch([
      db.prepare("UPDATE media_variants SET processing_state = 'ready' WHERE id = ?").bind(variant.id),
      db.prepare("UPDATE ingest_jobs SET state = 'completed', updated_at = CURRENT_TIMESTAMP WHERE id = ?").bind(job.id),
      auditStatement(db, 'wallpaper.upload_completed', 'wallpaper', job.wallpaper_id, { jobId: job.id, variantId: variant.id }),
    ]);
    return c.json({ jobId: job.id, state: 'completed' }, 201);
  });

  app.get('/v1/admin/jobs/:id', async (c) => {
    const db = createDb(c.env);
    if (!db) return c.json({ error: { code: 'not_configured', message: 'Database not configured.' } }, 501);
    const job = await db.prepare('SELECT id, wallpaper_id, state, error_code, created_at, updated_at FROM ingest_jobs WHERE id = ?').bind(c.req.param('id')).first();
    if (!job) return c.json({ error: { code: 'not_found', message: 'Job not found.' } }, 404);
    return c.json({ job });
  });

  app.get('/v1/admin/wallpapers', async (c) => {
    const db = createDb(c.env);
    if (!db) return c.json({ error: { code: 'not_configured', message: 'Database not configured.' } }, 501);
    const requestedVisibility = c.req.query('visibility');
    if (requestedVisibility !== undefined && !['draft', 'published', 'unpublished'].includes(requestedVisibility)) {
      return c.json({ error: { code: 'invalid_request', message: 'visibility must be draft, published, or unpublished.' } }, 400);
    }
    const limit = adminLimit(c.req.query('limit'));
    if (limit === null) return c.json({ error: { code: 'invalid_request', message: 'limit must be a positive integer.' } }, 400);
    const items = await listAdminWallpapers(db, requestedVisibility, limit);
    return c.json({ items: items ?? [], limit, visibility: requestedVisibility ?? 'all' });
  });

  app.patch('/v1/admin/wallpapers/:id', async (c) => {
    const db = createDb(c.env);
    if (!db) return c.json({ error: { code: 'not_configured', message: 'Database not configured.' } }, 501);
    const body = await c.req.json<WallpaperUpdateRequest>().catch(() => null);
    if (!body || typeof body !== 'object') return c.json({ error: { code: 'invalid_request', message: 'A JSON update is required.' } }, 400);
    const fields: string[] = [];
    const values: unknown[] = [];
    if (body.title !== undefined) {
      const title = typeof body.title === 'string' ? body.title.trim() : '';
      if (!title || title.length > 200) return c.json({ error: { code: 'invalid_request', message: 'title must be 1 to 200 characters.' } }, 400);
      fields.push('title = ?'); values.push(title);
    }
    if (body.description !== undefined) {
      if (typeof body.description !== 'string' || body.description.length > 4000) return c.json({ error: { code: 'invalid_request', message: 'description must be at most 4000 characters.' } }, 400);
      fields.push('description = ?'); values.push(body.description.trim());
    }
    if (body.featured !== undefined) {
      if (typeof body.featured !== 'boolean') return c.json({ error: { code: 'invalid_request', message: 'featured must be a boolean.' } }, 400);
      fields.push('featured = ?'); values.push(body.featured ? 1 : 0);
    }
    const thumbnailKey = validThumbnailKey(body.thumbnailKey);
    if (body.thumbnailKey !== undefined && thumbnailKey === undefined) {
      return c.json({ error: { code: 'invalid_request', message: 'thumbnailKey must be a thumbnails/ PNG, JPEG, or WebP object key.' } }, 400);
    }
    if (thumbnailKey !== undefined) {
      if (thumbnailKey && !c.env.R2_PUBLISHED) return c.json({ error: { code: 'not_configured', message: 'Published R2 binding is required to assign a thumbnail.' } }, 501);
      if (thumbnailKey && !await c.env.R2_PUBLISHED!.head(thumbnailKey)) {
        return c.json({ error: { code: 'invalid_request', message: 'thumbnailKey does not exist in published R2.' } }, 400);
      }
      fields.push('thumbnail_key = ?'); values.push(thumbnailKey);
    }
    if (fields.length === 0) return c.json({ error: { code: 'invalid_request', message: 'At least one editable field is required.' } }, 400);
    const id = c.req.param('id');
    const wallpaper = await db.prepare(`UPDATE wallpapers SET ${fields.join(', ')}, updated_at = CURRENT_TIMESTAMP WHERE id = ?
      RETURNING id, title, description, kind, visibility, premium, featured, thumbnail_key, created_at, updated_at`)
      .bind(...values, id).first<Record<string, unknown>>();
    if (!wallpaper) return c.json({ error: { code: 'not_found', message: 'Wallpaper not found.' } }, 404);
    await writeAudit(db, 'wallpaper.updated', 'wallpaper', id, { fields: fields.map(field => field.split(' ')[0]) });
    return c.json({ wallpaper });
  });

  app.put('/v1/admin/wallpapers/:id/thumbnail', async (c) => {
    const db = createDb(c.env);
    if (!db || !c.env.R2_PUBLISHED) return c.json({ error: { code: 'not_configured', message: 'Database and published R2 bindings are required.' } }, 501);
    const contentType = c.req.header('content-type')?.split(';')[0].toLowerCase() ?? '';
    const extension = imageExtension(contentType);
    const declaredLength = c.req.header('content-length');
    if (!extension || !declaredLength || !/^\d+$/.test(declaredLength) || Number(declaredLength) < 1 || Number(declaredLength) > maxThumbnailBytes || !c.req.raw.body) {
      return c.json({ error: { code: 'invalid_request', message: 'A PNG, JPEG, or WebP thumbnail no larger than 2 MiB is required.' } }, 400);
    }
    const id = c.req.param('id');
    const existing = await db.prepare('SELECT id, thumbnail_key FROM wallpapers WHERE id = ?').bind(id)
      .first<{ id: string; thumbnail_key: string | null }>();
    if (!existing) return c.json({ error: { code: 'not_found', message: 'Wallpaper not found.' } }, 404);
    const bytes = new Uint8Array(await c.req.raw.arrayBuffer());
    if (bytes.byteLength !== Number(declaredLength) || !isImage(bytes, extension)) {
      return c.json({ error: { code: 'invalid_upload', message: 'Thumbnail bytes do not match the declared image type and size.' } }, 400);
    }
    const thumbnailKey = `thumbnails/${id}/${crypto.randomUUID()}.${extension}`;
    await c.env.R2_PUBLISHED.put(thumbnailKey, bytes, { httpMetadata: { contentType } });
    await db.batch([
      db.prepare('UPDATE wallpapers SET thumbnail_key = ?, updated_at = CURRENT_TIMESTAMP WHERE id = ?').bind(thumbnailKey, id),
      auditStatement(db, 'wallpaper.thumbnail_uploaded', 'wallpaper', id, { thumbnailKey }),
    ]);
    if (existing.thumbnail_key) {
      const references = await db.prepare('SELECT COUNT(*) AS count FROM wallpapers WHERE thumbnail_key = ? AND id <> ?')
        .bind(existing.thumbnail_key, id).first<{ count: number }>();
      if (Number(references?.count ?? 0) === 0) await c.env.R2_PUBLISHED.delete(existing.thumbnail_key).catch(error => console.error('Unable to delete replaced thumbnail', error));
    }
    return c.json({ thumbnailUrl: `${new URL(c.req.url).origin}/v1/wallpapers/${encodeURIComponent(id)}/thumbnail` }, 201);
  });

  app.put('/v1/admin/wallpapers/:id/categories', async (c) => {
    const db = createDb(c.env);
    if (!db) return c.json({ error: { code: 'not_configured', message: 'Database not configured.' } }, 501);
    const body = await c.req.json<{ slugs?: unknown }>().catch(() => null);
    const slugs = body?.slugs;
    if (!Array.isArray(slugs) || slugs.length > 8 || slugs.some(slug => typeof slug !== 'string' || !/^[a-z0-9][a-z0-9-]{0,63}$/.test(slug))) {
      return c.json({ error: { code: 'invalid_request', message: 'slugs must contain up to eight category slugs.' } }, 400);
    }
    const uniqueSlugs = [...new Set(slugs)];
    const id = c.req.param('id');
    const wallpaper = await db.prepare('SELECT id FROM wallpapers WHERE id = ?').bind(id).first<{ id: string }>();
    if (!wallpaper) return c.json({ error: { code: 'not_found', message: 'Wallpaper not found.' } }, 404);
    const categories = uniqueSlugs.length === 0
      ? []
      : (await db.prepare(`SELECT id, slug FROM categories WHERE slug IN (${uniqueSlugs.map(() => '?').join(', ')})`).bind(...uniqueSlugs).all<{ id: string; slug: string }>()).results;
    if (categories.length !== uniqueSlugs.length) return c.json({ error: { code: 'invalid_request', message: 'One or more category slugs do not exist.' } }, 400);
    await db.batch([
      db.prepare('DELETE FROM wallpaper_categories WHERE wallpaper_id = ?').bind(id),
      ...categories.map(category => db.prepare('INSERT INTO wallpaper_categories (wallpaper_id, category_id) VALUES (?, ?)').bind(id, category.id)),
      auditStatement(db, 'wallpaper.categories_updated', 'wallpaper', id, { slugs: uniqueSlugs }),
    ]);
    return c.json({ wallpaperId: id, categories: uniqueSlugs });
  });

  app.post('/v1/admin/wallpapers/:id/publish', async (c) => {
    const db = createDb(c.env);
    if (!db) return c.json({ error: { code: 'not_configured', message: 'Database not configured.' } }, 501);
    const id = c.req.param('id');
    const job = await db.prepare('SELECT state FROM ingest_jobs WHERE wallpaper_id = ? ORDER BY updated_at DESC LIMIT 1').bind(id).first<{ state: string }>();
    if (job?.state !== 'completed') return c.json({ error: { code: 'invalid_state', message: 'Wallpaper has no completed ingest job; publish refused.' } }, 409);
    const updated = await db.prepare("UPDATE wallpapers SET visibility = 'published', updated_at = CURRENT_TIMESTAMP WHERE id = ? RETURNING id, visibility").bind(id).first();
    if (!updated) return c.json({ error: { code: 'not_found', message: 'Wallpaper not found.' } }, 404);
    await writeAudit(db, 'wallpaper.published', 'wallpaper', id, {});
    return c.json({ wallpaper: updated });
  });

  app.post('/v1/admin/wallpapers/:id/unpublish', async (c) => {
    const db = createDb(c.env);
    if (!db) return c.json({ error: { code: 'not_configured', message: 'Database not configured.' } }, 501);
    const id = c.req.param('id');
    const updated = await db.prepare("UPDATE wallpapers SET visibility = 'unpublished', updated_at = CURRENT_TIMESTAMP WHERE id = ? RETURNING id, visibility").bind(id).first();
    if (!updated) return c.json({ error: { code: 'not_found', message: 'Wallpaper not found.' } }, 404);
    await writeAudit(db, 'wallpaper.unpublished', 'wallpaper', id, {});
    return c.json({ wallpaper: updated });
  });

  app.delete('/v1/admin/wallpapers/:id', async (c) => {
    const db = createDb(c.env);
    if (!db) return c.json({ error: { code: 'not_configured', message: 'Database not configured.' } }, 501);
    const id = c.req.param('id');
    const wallpaper = await db.prepare('SELECT id, thumbnail_key FROM wallpapers WHERE id = ?').bind(id)
      .first<{ id: string; thumbnail_key: string | null }>();
    if (!wallpaper) return c.json({ error: { code: 'not_found', message: 'Wallpaper not found.' } }, 404);
    const variants = await db.prepare('SELECT object_key FROM media_variants WHERE wallpaper_id = ?').bind(id)
      .all<{ object_key: string }>();
    await db.batch([
      db.prepare('DELETE FROM ingest_jobs WHERE wallpaper_id = ?').bind(id),
      db.prepare('DELETE FROM wallpapers WHERE id = ?').bind(id),
      auditStatement(db, 'wallpaper.deleted', 'wallpaper', id, { variantCount: variants.results.length, hadThumbnail: Boolean(wallpaper.thumbnail_key) }),
    ]);
    let thumbnailIsShared = false;
    if (wallpaper.thumbnail_key) {
      const references = await db.prepare('SELECT COUNT(*) AS count FROM wallpapers WHERE thumbnail_key = ? AND id <> ?')
        .bind(wallpaper.thumbnail_key, id).first<{ count: number }>();
      thumbnailIsShared = Number(references?.count ?? 0) > 0;
    }
    const objectKeys = [...variants.results.map(variant => variant.object_key), ...(wallpaper.thumbnail_key && !thumbnailIsShared ? [wallpaper.thumbnail_key] : [])];
    let cleanupPending = false;
    if (objectKeys.length > 0 && c.env.R2_PUBLISHED) {
      const cleanup = await Promise.allSettled(objectKeys.map(objectKey => c.env.R2_PUBLISHED!.delete(objectKey)));
      cleanupPending = cleanup.some(result => result.status === 'rejected');
    } else if (objectKeys.length > 0) {
      cleanupPending = true;
    }
    return c.json({ deleted: true, cleanupPending });
  });

  return app;
}
