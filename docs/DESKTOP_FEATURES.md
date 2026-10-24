# Desktop app — complete feature list

All features below are implemented in code in this repository.
Windows build / desktop-attachment verification is still required
(see docs/TEST_PLAN.md) before any release claim.

## All pages (NavigationView in MainWindow)

- Home — Apply/Stop, video path, display summary, animated graphics banner (pulsing accent banner via Storyboard, stops with the page).
- Library — local MP4 library, refresh.
- Discover — bundled catalog (Premium Themes, Premium Glass, 4K Animated), refresh, premium flags.
- Downloads — downloaded files in LocalAppData downloads folder, with sizes, refresh.
- Displays — Win32 monitor count + primary size, refresh.
- Settings — all settings toggles + API URL + Save (local JSON only).
- Diagnostics — app version, OS version, renderer running or not, media folder, catalog found or not, refresh.
- About — product, stack, offline-first note, version.

## Animated UI / animated graphics

- EntranceThemeTransition on every page (fade/slide-in on navigation).
- Home animated banner: a Storyboard that pulses the banner opacity forever (AutoReverse) while the page is loaded, via AnimationService.Pulse.
- AnimationService.FadeIn for programmatic fade-ins.
- ListViews use the default WinUI item transitions; no external animation library is required.
- Animated wallpapers themselves are the main animated graphics: the renderer plays video behind icons, with pause/resume on fullscreen/lock/display-off/Battery Saver.

## Home / Apply

- Select a local H.264 MP4, Apply starts the native renderer behind desktop icons, Stop stops it.
- Safe fallback: if shell attachment is unsupported, the renderer exits with a clear error and leaves the desktop untouched — never a topmost fake wallpaper.

## Local library

- Lists real MP4 files in `%LOCALAPPDATA%\LiveWallpaperStudio\media`.
- Refresh button re-scans the folder; tapping an item selects it as the video to apply.

## Displays

- Monitor count and primary resolution read from Win32 `GetSystemMetrics` (SM_CMONITORS, SM_CXSCREEN, SM_CYSCREEN), shown on Home.
- Renderer enumerates displays with device name, bounds, DPI and primary flag (native DisplayManager).

## Discover / catalog

- Loads `assets/catalog.json` when present (bundled themes, Premium Themes, Premium Glass, 4K Animated).
- Shows title, theme and premium flag for each catalog item; tapping an item selects its video path when the file exists locally.
- When no catalog is found, Discover honestly says "not configured" — no fake content.

## Downloads

- DownloadManager (Core): streaming download to `.partial`, Range resume, jittered backoff (max 4 attempts), SHA-256 + size verification, atomic rename on success, bad checksum rejected.
- CacheService (Core): SHA-256-keyed names, LRU eviction only for unpinned / inactive / non-imported files.

## Settings

- Muted by default, pause on fullscreen / lock / display-off / battery saver, telemetry and crash reporting off by default, catalog off until an API URL is configured.
- Settings are stored locally as JSON in LocalAppData (`LocalSettingsService`), with Save from the Settings section. No settings are sent anywhere.

## Tray

- TrayService registers a system-tray icon via Win32 `Shell_NotifyIcon` and removes it on exit. Tray registration failure does not prevent the app from running.

## Diagnostics / safety

- Renderer supervisor: bounded restarts (max 3 unexpected exits), never elevated, never a Windows Service.
- Named-pipe IPC v1: hello, status, pause, resume, shutdown; 1 MiB message limit, method allowlist, UNC/HTTP paths rejected.
- Playback policy: pause on fullscreen-on-monitor, lock, display-off, Battery Saver, remote desktop.

## Still P1 (not in this build)

- WebView2 interactive wallpapers, playlists, premium sync, account favourites, signed installer. These are documented as not implemented, not shown as enabled features.
