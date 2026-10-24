-- Local SQLite schema, spec section 10. Applied transactionally on
-- startup, with a backup before any destructive migration.
-- Paths are cache-relative wherever possible; sensitive tokens are
-- NOT stored here — use OS-protected storage.

PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS schema_migrations (
    version INTEGER PRIMARY KEY,
    applied_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS wallpapers (
    id TEXT PRIMARY KEY,
    origin TEXT NOT NULL,
    title TEXT NOT NULL,
    kind TEXT NOT NULL,
    metadata_json TEXT NOT NULL DEFAULT '{}',
    added_at TEXT NOT NULL,
    last_used_at TEXT
);

CREATE TABLE IF NOT EXISTS assets (
    id TEXT PRIMARY KEY,
    wallpaper_id TEXT NOT NULL REFERENCES wallpapers(id) ON DELETE CASCADE,
    sha256 TEXT NOT NULL UNIQUE,
    relative_path TEXT NOT NULL,
    mime_type TEXT NOT NULL,
    codec TEXT NOT NULL,
    width INTEGER NOT NULL CHECK (width > 0),
    height INTEGER NOT NULL CHECK (height > 0),
    fps REAL NOT NULL CHECK (fps >= 0),
    bytes INTEGER NOT NULL CHECK (bytes > 0),
    verified_at TEXT
);

CREATE TABLE IF NOT EXISTS display_assignments (
    display_key TEXT PRIMARY KEY,
    wallpaper_id TEXT REFERENCES wallpapers(id),
    asset_id TEXT REFERENCES assets(id),
    fit_mode TEXT NOT NULL DEFAULT 'Fill',
    playback_json TEXT NOT NULL DEFAULT '{}',
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS favorite_wallpapers (
    wallpaper_id TEXT PRIMARY KEY REFERENCES wallpapers(id) ON DELETE CASCADE,
    created_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS collections (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    created_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS collection_items (
    collection_id TEXT NOT NULL REFERENCES collections(id) ON DELETE CASCADE,
    wallpaper_id TEXT NOT NULL REFERENCES wallpapers(id) ON DELETE CASCADE,
    sort_order INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (collection_id, wallpaper_id)
);

CREATE TABLE IF NOT EXISTS downloads (
    job_id TEXT PRIMARY KEY,
    asset_id TEXT NOT NULL,
    url_ref TEXT NOT NULL,
    etag TEXT,
    expected_sha256 TEXT,
    state TEXT NOT NULL,
    bytes_done INTEGER NOT NULL DEFAULT 0,
    bytes_total INTEGER,
    temp_path TEXT,
    failure_reason TEXT,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS cache_entries (
    sha256 TEXT PRIMARY KEY,
    relative_path TEXT NOT NULL,
    size_bytes INTEGER NOT NULL,
    last_accessed_at TEXT NOT NULL,
    pin_state TEXT NOT NULL DEFAULT 'unpinned'
);

CREATE TABLE IF NOT EXISTS settings (
    key TEXT PRIMARY KEY,
    json_value TEXT NOT NULL,
    updated_at TEXT NOT NULL
);
