PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS wallpapers (
  id TEXT PRIMARY KEY, title TEXT NOT NULL, description TEXT NOT NULL DEFAULT '',
  kind TEXT NOT NULL, visibility TEXT NOT NULL DEFAULT 'draft', premium INTEGER NOT NULL DEFAULT 0,
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP, updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);
CREATE TABLE IF NOT EXISTS media_variants (
  id TEXT PRIMARY KEY, wallpaper_id TEXT NOT NULL REFERENCES wallpapers(id) ON DELETE CASCADE,
  variant_key TEXT NOT NULL UNIQUE, object_key TEXT NOT NULL UNIQUE, mime_type TEXT NOT NULL,
  codec TEXT NOT NULL, width INTEGER NOT NULL, height INTEGER NOT NULL, fps REAL NOT NULL,
  size_bytes INTEGER NOT NULL, sha256 TEXT NOT NULL, processing_state TEXT NOT NULL,
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);
CREATE TABLE IF NOT EXISTS categories (id TEXT PRIMARY KEY, slug TEXT NOT NULL UNIQUE, display_name TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS wallpaper_categories (wallpaper_id TEXT NOT NULL REFERENCES wallpapers(id) ON DELETE CASCADE, category_id TEXT NOT NULL REFERENCES categories(id) ON DELETE CASCADE, PRIMARY KEY (wallpaper_id, category_id));
CREATE TABLE IF NOT EXISTS ingest_jobs (id TEXT PRIMARY KEY, wallpaper_id TEXT NOT NULL REFERENCES wallpapers(id), state TEXT NOT NULL, error_code TEXT, created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP, updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP);
CREATE INDEX IF NOT EXISTS wallpapers_public_recent_idx ON wallpapers (created_at DESC, id) WHERE visibility = 'published';
CREATE INDEX IF NOT EXISTS media_variants_wallpaper_idx ON media_variants(wallpaper_id);
