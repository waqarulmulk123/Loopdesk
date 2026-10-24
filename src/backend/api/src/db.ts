import { Pool } from 'pg';

// PostgreSQL via Hyperdrive (spec section 12). In Workers, the
// Hyperdrive binding is passed as the connection string; locally,
// DATABASE_URL is used. If neither is configured, every query
// function returns null and the API honestly reports an empty
// catalog instead of inventing content.

export type HyperdriveBinding = { connectionString: string };

export type DatabaseBindings = {
  // Hyperdrive is used in Workers. DATABASE_URL is only for local development
  // and must be supplied through .dev.vars rather than wrangler.jsonc.
  HYPERDRIVE?: HyperdriveBinding;
  DATABASE_URL?: string;
};

export type Db = Pool | null;

const pools = new Map<string, Pool>();

export function createPool(bindings: DatabaseBindings): Db {
  const connectionString = bindings.HYPERDRIVE?.connectionString ?? bindings.DATABASE_URL;
  if (!connectionString) return null;
  let pool = pools.get(connectionString);
  if (!pool) {
    pool = new Pool({ connectionString, max: 5 });
    pools.set(connectionString, pool);
  }
  return pool;
}

export type WallpaperRow = {
  id: string;
  title: string;
  description: string | null;
  kind: string;
  visibility: string;
  premium: boolean;
  created_at: string;
};

export async function listWallpapers(
  db: Db,
  limit: number,
  category?: string,
): Promise<WallpaperRow[] | null> {
  if (!db) return null;
  const params: unknown[] = [limit];
  let sql = `SELECT id, title, description, kind, visibility, premium, created_at
    FROM wallpapers WHERE visibility = 'published'`;
  if (category) {
    sql += ` AND id IN (SELECT wallpaper_id FROM wallpaper_categories WHERE category_id = $2)`;
    params.push(category);
  }
  sql += ` ORDER BY created_at DESC, id DESC LIMIT $1`;
  const result = await db.query(sql, params);
  return result.rows as WallpaperRow[];
}

export async function getWallpaper(db: Db, id: string): Promise<WallpaperRow | null> {
  if (!db) return null;
  const result = await db.query(
    `SELECT id, title, description, kind, visibility, premium, created_at
     FROM wallpapers WHERE id = $1 AND visibility = 'published'`,
    [id],
  );
  return (result.rows[0] as WallpaperRow | undefined) ?? null;
}

export async function listCategories(db: Db): Promise<{ id: string; slug: string; display_name: string }[] | null> {
  if (!db) return null;
  const result = await db.query(`SELECT id, slug, display_name FROM categories ORDER BY display_name ASC`);
  return result.rows as { id: string; slug: string; display_name: string }[];
}

export async function searchWallpapers(db: Db, q: string, limit: number): Promise<WallpaperRow[] | null> {
  if (!db) return null;
  const result = await db.query(
    `SELECT id, title, description, kind, visibility, premium, created_at
     FROM wallpapers WHERE visibility = 'published' AND title ILIKE $1
     ORDER BY created_at DESC, id DESC LIMIT $2`,
    [`%${q}%`, limit],
  );
  return result.rows as WallpaperRow[];
}
