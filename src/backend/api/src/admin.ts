import { Hono } from 'hono';
import { createDb, type DatabaseBindings } from './db.js';

type IngestMessage = { jobId: string; wallpaperId: string; variantId: string; sourceKey: string; targetKey: string };
type AdminBindings = DatabaseBindings & { ADMIN_API_TOKEN?: string; INGEST_QUEUE?: Queue<IngestMessage>; R2_INGEST?: R2Bucket };

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
    if (!db || !c.env.R2_INGEST || !c.env.INGEST_QUEUE) return c.json({ error: { code: 'not_configured', message: 'Database, R2, and Queue bindings are required.' } }, 501);
    const body = await c.req.json<{ title?: unknown; description?: unknown; premium?: unknown; contentType?: unknown }>().catch(() => null);
    const title = typeof body?.title === 'string' ? body.title.trim() : '';
    const contentType = typeof body?.contentType === 'string' ? body.contentType : '';
    if (!title || title.length > 200 || contentType !== 'video/mp4') return c.json({ error: { code: 'invalid_request', message: 'title (1-200 chars) and contentType video/mp4 are required.' } }, 400);
    const wallpaperId = crypto.randomUUID();
    const variantId = crypto.randomUUID();
    const jobId = crypto.randomUUID();
    const sourceKey = `ingest/${jobId}/source.mp4`;
    const targetKey = `published/${wallpaperId}/source.mp4`;
    await db.batch([
      db.prepare('INSERT INTO wallpapers (id, title, description, kind, visibility, premium) VALUES (?, ?, ?, ?, ?, ?)').bind(wallpaperId, title, typeof body?.description === 'string' ? body.description.slice(0, 4000) : '', 'video', 'draft', body?.premium === true ? 1 : 0),
      db.prepare('INSERT INTO media_variants (id, wallpaper_id, variant_key, object_key, mime_type, codec, width, height, fps, size_bytes, sha256, processing_state) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)').bind(variantId, wallpaperId, 'source', targetKey, 'video/mp4', 'h264', 0, 0, 0, 0, '', 'awaiting_upload'),
      db.prepare('INSERT INTO ingest_jobs (id, wallpaper_id, state) VALUES (?, ?, ?)').bind(jobId, wallpaperId, 'awaiting_upload'),
    ]);
    return c.json({ jobId, wallpaperId, variantId, uploadUrl: `${new URL(c.req.url).origin}/v1/admin/uploads/${jobId}/content` }, 201);
  });

  app.put('/v1/admin/uploads/:jobId/content', async (c) => {
    const db = createDb(c.env);
    if (!db || !c.env.R2_INGEST || !c.env.INGEST_QUEUE) return c.json({ error: { code: 'not_configured', message: 'Database, R2, and Queue bindings are required.' } }, 501);
    if (c.req.header('content-type')?.split(';')[0] !== 'video/mp4' || !c.req.raw.body) return c.json({ error: { code: 'invalid_request', message: 'A video/mp4 request body is required.' } }, 400);
    const job = await db.prepare('SELECT id, wallpaper_id, state FROM ingest_jobs WHERE id = ?').bind(c.req.param('jobId')).first<{ id: string; wallpaper_id: string; state: string }>();
    if (!job) return c.json({ error: { code: 'not_found', message: 'Upload job not found.' } }, 404);
    if (job.state !== 'awaiting_upload') return c.json({ error: { code: 'invalid_state', message: 'Upload content was already received.' } }, 409);
    const variant = await db.prepare('SELECT id, object_key FROM media_variants WHERE wallpaper_id = ?').bind(job.wallpaper_id).first<{ id: string; object_key: string }>();
    if (!variant) return c.json({ error: { code: 'invalid_state', message: 'Upload variant not found.' } }, 409);
    const sourceKey = `ingest/${job.id}/source.mp4`;
    await c.env.R2_INGEST.put(sourceKey, c.req.raw.body, { httpMetadata: { contentType: 'video/mp4' } });
    await db.prepare("UPDATE ingest_jobs SET state = 'queued', updated_at = CURRENT_TIMESTAMP WHERE id = ?").bind(job.id).run();
    await c.env.INGEST_QUEUE.send({ jobId: job.id, wallpaperId: job.wallpaper_id, variantId: variant.id, sourceKey, targetKey: variant.object_key });
    return c.json({ jobId: job.id, state: 'queued' }, 202);
  });

  app.get('/v1/admin/jobs/:id', async (c) => {
    const db = createDb(c.env);
    if (!db) return c.json({ error: { code: 'not_configured', message: 'Database not configured.' } }, 501);
    const job = await db.prepare('SELECT id, wallpaper_id, state, error_code, created_at, updated_at FROM ingest_jobs WHERE id = ?').bind(c.req.param('id')).first();
    if (!job) return c.json({ error: { code: 'not_found', message: 'Job not found.' } }, 404);
    return c.json({ job });
  });

  app.post('/v1/admin/wallpapers/:id/publish', async (c) => {
    const db = createDb(c.env);
    if (!db) return c.json({ error: { code: 'not_configured', message: 'Database not configured.' } }, 501);
    const id = c.req.param('id');
    const job = await db.prepare('SELECT state FROM ingest_jobs WHERE wallpaper_id = ? ORDER BY updated_at DESC LIMIT 1').bind(id).first<{ state: string }>();
    if (job?.state !== 'completed') return c.json({ error: { code: 'invalid_state', message: 'Wallpaper has no completed ingest job; publish refused.' } }, 409);
    const updated = await db.prepare("UPDATE wallpapers SET visibility = 'published', updated_at = CURRENT_TIMESTAMP WHERE id = ? RETURNING id, visibility").bind(id).first();
    if (!updated) return c.json({ error: { code: 'not_found', message: 'Wallpaper not found.' } }, 404);
    return c.json({ wallpaper: updated });
  });

  app.post('/v1/admin/wallpapers/:id/unpublish', async (c) => {
    const db = createDb(c.env);
    if (!db) return c.json({ error: { code: 'not_configured', message: 'Database not configured.' } }, 501);
    const updated = await db.prepare("UPDATE wallpapers SET visibility = 'unpublished', updated_at = CURRENT_TIMESTAMP WHERE id = ? RETURNING id, visibility").bind(c.req.param('id')).first();
    if (!updated) return c.json({ error: { code: 'not_found', message: 'Wallpaper not found.' } }, 404);
    return c.json({ wallpaper: updated });
  });

  return app;
}
