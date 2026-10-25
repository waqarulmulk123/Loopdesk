# Desktop app — implemented feature list

This document describes the shipped cloud-only desktop flow.

## All pages (NavigationView in MainWindow)

- Home — cloud-first product overview and display summary.
- My wallpapers — protected Loopdesk cloud downloads, preview, apply, stop, search and refresh.
- Discover — production Cloudflare catalog, search, favourite and download.
- Downloads — protected download records, refresh.
- Displays — Win32 monitor count + primary size, refresh.
- Settings — local preferences; runtime renderer policy integration is not implemented.
- Diagnostics — app version, OS version, renderer status, storage posture and catalog status.
- About — product, stack, offline-first note, version.

## Animated UI / animated graphics

- EntranceThemeTransition on every page (fade/slide-in on navigation).
- Home animated banner: a Storyboard that pulses the banner opacity forever (AutoReverse) while the page is loaded, via AnimationService.Pulse.
- AnimationService.FadeIn for programmatic fade-ins.
- ListViews use the default WinUI item transitions; no external animation library is required.
- Animated wallpapers are played by the native renderer behind desktop icons on the primary display.

## Apply

- Download a free Cloudflare catalog item, then preview or apply it from My wallpapers.
- The application never imports or scans arbitrary drive folders.
- Safe fallback: if shell attachment is unsupported, the renderer exits with a clear error and leaves the desktop untouched — never a topmost fake wallpaper.

## Displays

- Monitor count and primary resolution read from Win32 `GetSystemMetrics` (SM_CMONITORS, SM_CXSCREEN, SM_CYSCREEN), shown on Home.
- Renderer enumerates displays with device name, bounds, DPI and primary flag; playback applies to the primary display only.

## Discover / catalog

- Loads the production Cloudflare catalog only; packaged posters are UI artwork, not local video catalog content.

## Downloads

- DownloadManager verifies SHA-256 and size before a download enters the protected per-user vault.

## Settings

- Preferences are stored locally as JSON in LocalAppData. Runtime pause/mute integration is not implemented yet.

## Tray

- A tray/background wallpaper lifecycle is not implemented. Closing Loopdesk stops the renderer.

## Diagnostics / safety

- Renderer supervisor stops the native renderer when Loopdesk exits; it is never elevated and never a Windows Service.
- Named-pipe IPC v1 supports status, pause, resume and shutdown; policy/event integration is not implemented.

## Still P1 (not in this build)

- Multi-monitor assignment, persistence after app exit, power/session pause policy, WebView2 interactive wallpapers, playlists, premium sync, account favourites, and signed direct installation.
