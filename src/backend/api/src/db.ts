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
  categories: string[];
  width: number | null;
  height: number | null;
};

function wallpaper(row: Record<string, unknown>): WallpaperRow {
  const categoryNames = typeof row.category_names === 'string' && row.category_names.length > 0
    ? row.category_names.split('|')
    : [];
  return {
    id: String(row.id), title: String(row.title), description: row.description as string | null,
    kind: String(row.kind), visibility: String(row.visibility), premium: Boolean(row.premium),
    created_at: String(row.created_at), categories: categoryNames,
    width: typeof row.width === 'number' ? row.width : null,
    height: typeof row.height === 'number' ? row.height : null,
  };
}

export async function listWallpapers(db: Db, limit: number, category?: string): Promise<WallpaperRow[] | null> {
  if (!db) return null;
  const columns = `w.id, w.title, w.description, w.kind, w.visibility, w.premium, w.created_at,
    COALESCE((SELECT group_concat(c.display_name, '|') FROM wallpaper_categories wc JOIN categories c ON c.id = wc.category_id WHERE wc.wallpaper_id = w.id), '') AS category_names,
    (SELECT mv.width FROM media_variants mv WHERE mv.wallpaper_id = w.id AND mv.processing_state = 'ready' AND mv.mime_type = 'video/mp4' AND mv.codec = 'h264' ORDER BY mv.width * mv.height DESC LIMIT 1) AS width,
    (SELECT mv.height FROM media_variants mv WHERE mv.wallpaper_id = w.id AND mv.processing_state = 'ready' AND mv.mime_type = 'video/mp4' AND mv.codec = 'h264' ORDER BY mv.width * mv.height DESC LIMIT 1) AS height`;
  const query = category
    ? `SELECT ${columns} FROM wallpapers w WHERE w.visibility = 'published' AND w.id IN (SELECT wallpaper_id FROM wallpaper_categories WHERE category_id = ?) ORDER BY w.created_at DESC, w.id DESC LIMIT ?`
    : `SELECT ${columns} FROM wallpapers w WHERE w.visibility = 'published' ORDER BY w.created_at DESC, w.id DESC LIMIT ?`;
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
  const escaped = q.replace(/[\\%_]/g, character => `\\${character}`);
  const result = await db.prepare("SELECT id, title, description, kind, visibility, premium, created_at FROM wallpapers WHERE visibility = 'published' AND title LIKE ? ESCAPE '\\' ORDER BY created_at DESC, id DESC LIMIT ?").bind(`%${escaped}%`, limit).all<Record<string, unknown>>();
  return result.results.map(wallpaper);
}
