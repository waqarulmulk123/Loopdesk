-- Public catalog metadata and the append-only record of privileged mutations.
-- This migration is additive so existing production catalogs retain their IDs
-- and media objects.
ALTER TABLE wallpapers ADD COLUMN thumbnail_key TEXT;
ALTER TABLE wallpapers ADD COLUMN featured INTEGER NOT NULL DEFAULT 0;

CREATE TABLE IF NOT EXISTS admin_audit_log (
  id TEXT PRIMARY KEY,
  action TEXT NOT NULL,
  target_type TEXT NOT NULL,
  target_id TEXT NOT NULL,
  detail_json TEXT NOT NULL DEFAULT '{}',
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS wallpapers_public_featured_idx
  ON wallpapers (featured DESC, created_at DESC, id)
  WHERE visibility = 'published';
CREATE INDEX IF NOT EXISTS admin_audit_log_target_idx
  ON admin_audit_log (target_type, target_id, created_at DESC);
