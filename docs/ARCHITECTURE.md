# Architecture

See `ANALYSIS.md` for the full reasoning and research verification. This file is the short, build-time summary.

## Process model

```text
WinUI 3 app  <-->  Renderer supervisor  <-->  Native C++ renderer (one per user session)
     |                    |                              |
     v                    v                              v
SQLite library     Named pipes (ACL, versioned)     Per-monitor HWNDs, D3D11, Media Foundation
Downloads (HttpClient, SHA-256)  -->  Verified local cache  -->  Renderer
Optional: HTTPS  -->  Cloudflare Workers/Hono  -->  D1 catalog, R2, Queues
```

- UI owns browsing, local state, downloads, settings and optional sign-in. It never blocks on decode, network or disk.
- Supervisor starts/restarts the renderer in the interactive user session and watches heartbeats. It is not a permanently elevated Windows Service.
- Renderer owns per-monitor wallpaper HWNDs, graphics devices, decoding, frame pacing and policy. It never accepts executable code or unvalidated paths from IPC, and never loads WinUI assemblies or calls the cloud API.
- Backend is content catalog and distribution only. Local playback must not depend on it.

## Desktop attachment

- Interface: `IDesktopShellAdapter` — probe the shell hierarchy, validate safety, attach, detach, and recover on Explorer restart (`TaskbarCreated`).
- Safety bar: icons remain visible, selectable and right-clickable; Start, taskbar and normal windows keep working; wallpaper stays behind icons and never steals focus or intercepts clicks; invalid attachment aborts to a normal desktop.
- Fallback: if no strategy passes, return `ShellAttachmentUnsupported` and show a static-wallpaper fallback. Never a topmost fullscreen imitation.

## Playback and power

- States: Stopped → Preparing → Attaching → Playing → Paused → Stopping → Stopped, plus Recovering and Failed.
- Default pause: system lock, display off, sleep, fullscreen app on that monitor; optional battery, Battery Saver and remote-desktop rules.
- Paused means paused: no decoding of unseen frames.
- Loop: seek to start on media end, avoid reinitialisation, validate the loop join; zero visible black frames is the goal, not a guarantee for every asset.

## IPC

See `src/shared/contracts/protocol.schema.json`. Envelope: protocolVersion, requestId, kind, method, payload. Methods include `engine.hello`, `engine.status`, `engine.diagnostics`, `displays.list`, `wallpaper.apply`, `wallpaper.remove`, `wallpaper.setLayout`, `playback.pause/resume/setVolume/setRate`, `settings.setPolicy`, `events.subscribe`. Maximum control message 1 MiB. Paths are validated against the app cache or a user-approved import; network/UNC paths are rejected by default.

## Local data

`%LOCALAPPDATA%\LiveWallpaperStudio\` — `data/state.db`, `media/<sha256>.<ext>`, `previews/`, `temp/*.partial`, `logs/`, `diagnostics/`. Downloads stream to `.partial`, resume with Range/ETag where supported, verify SHA-256 and size, then atomically rename. Cache eviction is LRU only for unpinned, inactive, non-imported items.

## Cloud

- Public: immutable versioned assets on a custom CDN domain after publication.
- Premium: private R2 + short-lived authorized delivery via the S3 API endpoint or an authenticated Worker/custom-domain mechanism with authorization-aware cache keys. Never cache premium content under a public shared key.
- Ingest: admin upload intent → private staging → Queue → FFmpeg worker → validation → publish. Never transcode 4K inside a Worker request.
- D1 holds current catalog metadata. Accounts, purchases, and premium entitlements are deliberately not implemented until an identity and transactional-entitlement design is approved.
- API: `/v1/health`, `/v1/config`, `/v1/wallpapers`, `/v1/wallpapers/:id`, `/v1/categories`, `/v1/search`, `/v1/assets/:variantId/authorize-download`, authenticated `/v1/me/favorites`, admin upload/publish/audit routes.
