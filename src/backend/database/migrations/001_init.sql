-- PostgreSQL catalog schema, spec section 12 (minimum canonical schema).
-- Account/entitlement tables are added only with AUTH-001 / PAY-001.
-- Never store payment card details here.

CREATE TABLE IF NOT EXISTS creators (
  id UUID PRIMARY KEY,
  display_name TEXT NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS wallpapers (
  id UUID PRIMARY KEY,
  creator_id UUID REFERENCES creators(id),
  title TEXT NOT NULL CHECK (length(title) BETWEEN 1 AND 200),
  description TEXT NOT NULL DEFAULT '',
  kind TEXT NOT NULL CHECK (kind IN ('video', 'static', 'html', 'shader')),
  visibility TEXT NOT NULL DEFAULT 'draft'
    CHECK (visibility IN ('draft', 'review', 'published', 'unpublished', 'rejected')),
  premium BOOLEAN NOT NULL DEFAULT FALSE,
  thumbnail_key TEXT,
  preview_key TEXT,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  published_at TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS media_variants (
  id UUID PRIMARY KEY,
  wallpaper_id UUID NOT NULL REFERENCES wallpapers(id) ON DELETE CASCADE,
  variant_key TEXT NOT NULL UNIQUE,
  object_key TEXT NOT NULL UNIQUE,
  mime_type TEXT NOT NULL,
  codec TEXT NOT NULL,
  width INTEGER NOT NULL CHECK (width > 0),
  height INTEGER NOT NULL CHECK (height > 0),
  fps NUMERIC(8, 3) NOT NULL CHECK (fps >= 0),
  size_bytes BIGINT NOT NULL CHECK (size_bytes > 0),
  sha256 CHAR(64) NOT NULL,
  processing_state TEXT NOT NULL CHECK (processing_state IN ('pending', 'ready', 'failed')),
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS categories (
  id UUID PRIMARY KEY,
  slug TEXT NOT NULL UNIQUE,
  display_name TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS wallpaper_categories (
  wallpaper_id UUID NOT NULL REFERENCES wallpapers(id) ON DELETE CASCADE,
  category_id UUID NOT NULL REFERENCES categories(id) ON DELETE CASCADE,
  PRIMARY KEY (wallpaper_id, category_id)
);

CREATE TABLE IF NOT EXISTS tags (
  id UUID PRIMARY KEY,
  slug TEXT NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS wallpaper_tags (
  wallpaper_id UUID NOT NULL REFERENCES wallpapers(id) ON DELETE CASCADE,
  tag_id UUID NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
  PRIMARY KEY (wallpaper_id, tag_id)
);

CREATE TABLE IF NOT EXISTS ingest_jobs (
  id UUID PRIMARY KEY,
  wallpaper_id UUID NOT NULL REFERENCES wallpapers(id),
  idempotency_key TEXT NOT NULL UNIQUE,
  state TEXT NOT NULL,
  attempt_count INTEGER NOT NULL DEFAULT 0,
  error_code TEXT,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS audit_events (
  id UUID PRIMARY KEY,
  actor_id TEXT NOT NULL,
  action TEXT NOT NULL,
  entity_id TEXT,
  details JSONB NOT NULL DEFAULT '{}'::jsonb,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS wallpapers_public_recent_idx
  ON wallpapers (published_at DESC, id)
  WHERE visibility = 'published';
CREATE INDEX IF NOT EXISTS media_variants_wallpaper_idx ON media_variants(wallpaper_id);
CREATE INDEX IF NOT EXISTS ingest_jobs_state_idx ON ingest_jobs(state, created_at);
