import { Hono } from 'hono';
import { createPool, type DatabaseBindings } from './db.js';

// Admin routes (spec section 11). These routes are NOT a security
// boundary on their own: the Worker must be deployed behind OIDC
// auth with an admin role check (see wrangler.jsonc / DEPLOYMENT.md).
// Without a database, they return 501, not fake success. Publish
// only flips visibility when every required variant is present and
// validated; the full variant check lives in the processing job's
// ingest_jobs record, and publish refuses to run if the job is not
// in a completed state.

export function adminRoutes(): Hono<{ Bindings: DatabaseBindings }> {
  const app = new Hono<{ Bindings: DatabaseBindings }>();
  const dbFor = (c: { env: DatabaseBindings }) => createPool(c.env);

  app.post('/v1/admin/uploads/init', (c) =>
    c.json({ error: { code: 'not_implemented', message: 'Upload intent requires R2 + auth configuration.' } }, 501),
  );

  app.post('/v1/admin/uploads/complete', (c) =>
    c.json({ error: { code: 'not_implemented', message: 'Upload completion requires R2 + Queues configuration.' } }, 501),
  );

  app.get('/v1/admin/jobs/:id', async (c) => {
    const db = dbFor(c);
    if (!db) return c.json({ error: { code: 'not_configured', message: 'Database not configured.' } }, 501);
    const result = await db.query(
      `SELECT id, wallpaper_id, state, error_code, created_at, updated_at FROM ingest_jobs WHERE id = $1`,
      [c.req.param('id')],
    );
    const job = result.rows[0];
    if (!job) return c.json({ error: { code: 'not_found', message: 'Job not found.' } }, 404);
    return c.json({ job });
  });

  app.post('/v1/admin/wallpapers/:id/publish', async (c) => {
    const db = dbFor(c);
    if (!db) return c.json({ error: { code: 'not_configured', message: 'Database not configured.' } }, 501);
    const id = c.req.param('id');
    const job = await db.query(
      `SELECT state FROM ingest_jobs WHERE wallpaper_id = $1 ORDER BY updated_at DESC LIMIT 1`,
      [id],
    );
    if (job.rows[0]?.state !== 'completed') {
      return c.json(
        { error: { code: 'invalid_state', message: 'Wallpaper has no completed ingest job; publish refused.' } },
        409,
      );
    }
    const updated = await db.query(
      `UPDATE wallpapers SET visibility = 'published', updated_at = NOW() WHERE id = $1 RETURNING id, visibility`,
      [id],
    );
    if (!updated.rows[0]) return c.json({ error: { code: 'not_found', message: 'Wallpaper not found.' } }, 404);
    return c.json({ wallpaper: updated.rows[0] });
  });

  app.post('/v1/admin/wallpapers/:id/unpublish', async (c) => {
    const db = dbFor(c);
    if (!db) return c.json({ error: { code: 'not_configured', message: 'Database not configured.' } }, 501);
    const updated = await db.query(
      `UPDATE wallpapers SET visibility = 'unpublished', updated_at = NOW() WHERE id = $1 RETURNING id, visibility`,
      [c.req.param('id')],
    );
    if (!updated.rows[0]) return c.json({ error: { code: 'not_found', message: 'Wallpaper not found.' } }, 404);
    return c.json({ wallpaper: updated.rows[0] });
  });

  return app;
}
