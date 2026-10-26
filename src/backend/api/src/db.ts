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

export type WallpaperPage = {
  items: WallpaperRow[];
  nextCursor: string | null;
};

type Cursor = { createdAt: string; id: string };

// A cursor is opaque to API consumers, but deliberately contains no secret.
// It is only the stable ordering key for a published catalog row.
export function encodeCursor(createdAt: string, id: string): string {
  return btoa(`${createdAt}\n${id}`)
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=+$/, '');
}

export function decodeCursor(value: string | undefined): Cursor | null | undefined {
  if (value === undefined) return undefined;
  if (!/^[A-Za-z0-9_-]{1,512}$/.test(value)) return null;
  try {
    const padded = value.replace(/-/g, '+').replace(/_/g, '/') + '='.repeat((4 - value.length % 4) % 4);
    const [createdAt, id, extra] = atob(padded).split('\n');
    if (extra !== undefined || !createdAt || !id || createdAt.length > 128 || id.length > 128) return null;
    return { createdAt, id };
  } catch {
    return null;
  }
}

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

export async function listWallpapers(db: Db, limit: number, category?: string, cursor?: Cursor): Promise<WallpaperPage | null> {
  if (!db) return null;
  const columns = `w.id, w.title, w.description, w.kind, w.visibility, w.premium, w.created_at,
    COALESCE((SELECT group_concat(c.display_name, '|') FROM wallpaper_categories wc JOIN categories c ON c.id = wc.category_id WHERE wc.wallpaper_id = w.id), '') AS category_names,
    (SELECT mv.width FROM media_variants mv WHERE mv.wallpaper_id = w.id AND mv.processing_state = 'ready' AND mv.mime_type = 'video/mp4' AND mv.codec = 'h264' ORDER BY mv.width * mv.height DESC LIMIT 1) AS width,
    (SELECT mv.height FROM media_variants mv WHERE mv.wallpaper_id = w.id AND mv.processing_state = 'ready' AND mv.mime_type = 'video/mp4' AND mv.codec = 'h264' ORDER BY mv.width * mv.height DESC LIMIT 1) AS height`;
  const cursorClause = cursor ? ' AND (w.created_at < ? OR (w.created_at = ? AND w.id < ?))' : '';
  const query = category
    ? `SELECT ${columns} FROM wallpapers w WHERE w.visibility = 'published' AND w.id IN (SELECT wallpaper_id FROM wallpaper_categories WHERE category_id = ?)${cursorClause} ORDER BY w.created_at DESC, w.id DESC LIMIT ?`
    : `SELECT ${columns} FROM wallpapers w WHERE w.visibility = 'published'${cursorClause} ORDER BY w.created_at DESC, w.id DESC LIMIT ?`;
  const bindings = category
    ? [category, ...(cursor ? [cursor.createdAt, cursor.createdAt, cursor.id] : []), limit + 1]
    : [...(cursor ? [cursor.createdAt, cursor.createdAt, cursor.id] : []), limit + 1];
  const statement = db.prepare(query).bind(...bindings);
  const result = await statement.all<Record<string, unknown>>();
  const rows = result.results.map(wallpaper);
  const hasMore = rows.length > limit;
  const items = hasMore ? rows.slice(0, limit) : rows;
  const last = items.at(-1);
  return { items, nextCursor: hasMore && last ? encodeCursor(last.created_at, last.id) : null };
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

export async function searchWallpapers(db: Db, q: string, limit: number, cursor?: Cursor): Promise<WallpaperPage | null> {
  if (!db) return null;
  const escaped = q.replace(/[\\%_]/g, character => `\\${character}`);
  const cursorClause = cursor ? ' AND (created_at < ? OR (created_at = ? AND id < ?))' : '';
  const result = await db.prepare(`SELECT id, title, description, kind, visibility, premium, created_at FROM wallpapers WHERE visibility = 'published' AND title LIKE ? ESCAPE '\\'${cursorClause} ORDER BY created_at DESC, id DESC LIMIT ?`)
    .bind(`%${escaped}%`, ...(cursor ? [cursor.createdAt, cursor.createdAt, cursor.id] : []), limit + 1)
    .all<Record<string, unknown>>();
  const rows = result.results.map(wallpaper);
  const hasMore = rows.length > limit;
  const items = hasMore ? rows.slice(0, limit) : rows;
  const last = items.at(-1);
  return { items, nextCursor: hasMore && last ? encodeCursor(last.created_at, last.id) : null };
}
