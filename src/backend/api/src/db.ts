export type DatabaseBindings = {
  DB?: D1Database;
};

export type Db = D1Database | null;

export function createDb(bindings: DatabaseBindings): Db {
  return bindings.DB ?? null;
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

function wallpaper(row: Record<string, unknown>): WallpaperRow {
  return { ...row, premium: Boolean(row.premium) } as WallpaperRow;
}

export async function listWallpapers(db: Db, limit: number, category?: string): Promise<WallpaperRow[] | null> {
  if (!db) return null;
  const query = category
    ? "SELECT id, title, description, kind, visibility, premium, created_at FROM wallpapers WHERE visibility = 'published' AND id IN (SELECT wallpaper_id FROM wallpaper_categories WHERE category_id = ?) ORDER BY created_at DESC, id DESC LIMIT ?"
    : "SELECT id, title, description, kind, visibility, premium, created_at FROM wallpapers WHERE visibility = 'published' ORDER BY created_at DESC, id DESC LIMIT ?";
  const statement = category ? db.prepare(query).bind(category, limit) : db.prepare(query).bind(limit);
  const result = await statement.all<Record<string, unknown>>();
  return result.results.map(wallpaper);
}

export async function getWallpaper(db: Db, id: string): Promise<WallpaperRow | null> {
  if (!db) return null;
  const row = await db.prepare("SELECT id, title, description, kind, visibility, premium, created_at FROM wallpapers WHERE id = ? AND visibility = 'published'").bind(id).first<Record<string, unknown>>();
  return row ? wallpaper(row) : null;
}

export async function listCategories(db: Db): Promise<{ id: string; slug: string; display_name: string }[] | null> {
  if (!db) return null;
  const result = await db.prepare("SELECT id, slug, display_name FROM categories ORDER BY display_name ASC").all<{ id: string; slug: string; display_name: string }>();
  return result.results;
}

export async function searchWallpapers(db: Db, q: string, limit: number): Promise<WallpaperRow[] | null> {
  if (!db) return null;
  const result = await db.prepare("SELECT id, title, description, kind, visibility, premium, created_at FROM wallpapers WHERE visibility = 'published' AND title LIKE ? ORDER BY created_at DESC, id DESC LIMIT ?").bind(`%${q}%`, limit).all<Record<string, unknown>>();
  return result.results.map(wallpaper);
}
