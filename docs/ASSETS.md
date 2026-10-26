# Premium Themes + Premium Glass + 4K Animated — bundled assets

Generated 2026-10-24 for this project. All 16 themes are marked
premium in the catalog, and all are animated video wallpapers.

## 4K Animated (new)

Important honesty note: these 4K files are upscaled to 3840x2160
from generated 1280x720 sources with ffmpeg Lanczos (preset
veryfast, CRF 28), verified with ffprobe. They are 4K resolution,
not native 4K capture. The source note is also stored in
catalog.json for each 4K item.

| Theme | Video (3840x2160, H.264, 24 fps, 10 s loop) | Poster |
|---|---|---|
| 4K Alpine Lake Sunrise | assets/wallpapers/video/4k/media-generation-alpine-lake-sunrise-*-4k.mp4 | assets/wallpapers/video/.thumbnails/media-generation-alpine-lake-sunrise-*.jpg |
| 4K Tokyo Rain Night | assets/wallpapers/video/4k/media-generation-tokyo-rain-night-*-4k.mp4 | assets/wallpapers/video/.thumbnails/media-generation-tokyo-rain-night-*.jpg |
| 4K Jungle Waterfall | assets/wallpapers/video/4k/media-generation-jungle-waterfall-*-4k.mp4 | assets/wallpapers/video/.thumbnails/media-generation-jungle-waterfall-*.jpg |
| 4K Earth from Orbit | assets/wallpapers/video/4k/media-generation-earth-orbit-*-4k.mp4 | assets/wallpapers/video/.thumbnails/media-generation-earth-orbit-*.jpg |

## Premium Glass (new)

| Theme | Video (1280x720, H.264, 24 fps, 10 s loop) | Poster |
|---|---|---|
| Glass City | assets/wallpapers/video/media-generation-glass-city-*.mp4 | assets/wallpapers/posters/media-generation-poster-glass-city-*.webp |
| Glass Aurora | assets/wallpapers/video/media-generation-glass-aurora-*.mp4 | assets/wallpapers/posters/media-generation-poster-glass-aurora-*.webp |
| Glass Ocean | assets/wallpapers/video/media-generation-glass-ocean-*.mp4 | assets/wallpapers/posters/media-generation-poster-glass-ocean-*.webp |
| Glass Galaxy | assets/wallpapers/video/media-generation-glass-galaxy-*.mp4 | assets/wallpapers/posters/media-generation-poster-glass-galaxy-*.webp |
| Glass Flowers | assets/wallpapers/video/media-generation-glass-flowers-*.mp4 | assets/wallpapers/video/.thumbnails/media-generation-glass-flowers-*.jpg |
| Glass Abstract | assets/wallpapers/video/media-generation-glass-abstract-*.mp4 | assets/wallpapers/video/.thumbnails/media-generation-glass-abstract-*.jpg |

## Premium Themes (first pack)

| Theme | Video (1280x720, H.264, 24 fps, 10 s loop) | Poster |
|---|---|---|
| Neon Cyberpunk City | assets/wallpapers/video/media-generation-neon-cyberpunk-city-*.mp4 | assets/wallpapers/posters/media-generation-poster-neon-cyberpunk-city-*.webp |
| Aurora over the Mountains | assets/wallpapers/video/media-generation-aurora-mountains-*.mp4 | assets/wallpapers/posters/media-generation-poster-aurora-mountains-*.webp |
| Ocean Sunset | assets/wallpapers/video/media-generation-ocean-sunset-*.mp4 | assets/wallpapers/posters/media-generation-poster-ocean-sunset-*.webp |
| Galaxy Nebula | assets/wallpapers/video/media-generation-galaxy-nebula-*.mp4 | assets/wallpapers/posters/media-generation-poster-galaxy-nebula-*.webp |
| Sakura Garden | assets/wallpapers/video/media-generation-sakura-garden-*.mp4 | assets/wallpapers/video/.thumbnails/media-generation-sakura-garden-*.jpg |
| Fluid Gradient | assets/wallpapers/video/media-generation-fluid-gradient-*.mp4 | assets/wallpapers/video/.thumbnails/media-generation-fluid-gradient-*.jpg |

Icons (WebP, square):

- assets/icons/media-generation-app-icon-*.webp — app logo
- assets/icons/media-generation-premium-crown-*.webp — premium badge
- assets/icons/media-generation-library-icon-*.webp — library section

## Added to the database

- `assets/catalog.json` — the full catalog, with SHA-256, size, codec, dimensions and file paths for every theme.
- `assets/catalog.db` — a ready SQLite database built from the local schema (`src/desktop/LiveWallpaperStudio.Infrastructure/Sqlite/schema.sql`), containing 16 wallpapers, 16 video assets, the "Premium Themes", "Premium Glass" and "4K Animated" collections, and pinned cache entries. Verified: 16 wallpapers, 16 assets.
- `src/desktop/LiveWallpaperStudio.Infrastructure/Sqlite/seed.sql` — the same seed as SQL, to apply after schema.sql.
- `src/backend/database/migrations/002_seed_premium_themes.sql`, `003_seed_premium_glass.sql`, and `004_seed_4k_animated.sql` are legacy PostgreSQL seed drafts. They are not applied to the deployed D1 catalog and must not be used for production until they are ported, processed, rights-reviewed, and published through the ingest workflow.

These files are source and publishing assets. The consumer desktop app does not import or scan them; it displays only cloud-catalog downloads protected in its per-user vault. Publish owned, rights-reviewed H.264 MP4 assets through the authenticated backend workflow before they can appear in Discover.
