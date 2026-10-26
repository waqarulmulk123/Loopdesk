# Loopdesk

A native Windows live-wallpaper application.

## What it is

- WinUI 3 desktop app for browsing, downloading, previewing, and applying wallpapers
- Native C++ renderer that plays video **behind desktop icons** on the primary display
- Cloudflare-backed catalog: Workers + D1 + R2

## Finalized stack

| Layer | Choice |
|---|---|
| UI | WinUI 3, XAML, C# 14, .NET 10 LTS, Windows App SDK 2.5.1 stable |
| Renderer | Native C++20/23, Win32, COM, Direct3D 11, DXGI |
| Video | Media Foundation first, libmpv as an interface-based fallback |
| Interactive wallpapers (P1) | WebView2, isolated renderer |
| IPC | Per-user, ACL-restricted Windows named pipes, versioned JSON (see `src/shared/contracts/protocol.schema.json`) |
| Local state | SQLite, WAL |
| Backend | TypeScript + Cloudflare Workers + Hono |
| Catalog database | Cloudflare D1 (catalog metadata) |
| Storage / delivery | Cloudflare R2 served through the Workers API |
| Publishing | Local FFmpeg preparation + authenticated direct MP4 publishing |
| CI / distribution | GitHub Actions, MSIX for Store, signed direct installer |

## Key architecture decisions

- **One renderer process per user session**, not one per monitor. Start there; add isolation only if benchmarks justify it.
- **D3D11 first.** Add D3D12 only for a proven, measured requirement.
- **Desktop attachment is undocumented and version-sensitive**, especially Windows 11 24H2. Use a version-adaptive `DesktopShellAdapter` with runtime detection, Explorer-restart recovery, and a safe static-wallpaper fallback. Never cover icons or render a topmost fullscreen imitation.
- **Premium delivery is not "signed R2 URL on a custom domain".** R2 S3 presigned URLs work through the S3 API endpoint, not custom domains. Public assets go on a custom CDN domain; premium assets use private R2 with short-lived authorized delivery and cache keys that respect authorization.
- **D1 is the deployed public-catalog database.** It is appropriate for the read-mostly catalog. A future accounts, purchases, and complex-entitlement service needs a separately designed transactional datastore; it is not present in this repository.
- **Legacy Windows 7/8 is a separate edition**, not v1. The package technical floor is Windows 10 1809 (build 17763), but release validation targets Windows 10 22H2 (build 19045) and Windows 11 22H2 or later. .NET 10 does not support Windows 7/8.1.
- **Signing does not guarantee no SmartScreen prompt.** Azure Artifact Signing (formerly Trusted Signing) eligibility is geographically restricted — check before depending on it.

## Build order (spec milestones)

M0 environment/feasibility → M1 minimal real desktop video wallpaper → M2 reliable engine + multi-monitor → M3 full offline app → M4 real Cloudflare backend → M5 production UX, security, install and tests → M6 P1 extensions.

## Important limitation in this workspace

The desktop product requires Windows CI and physical Windows validation for release. The GitHub Windows build verifies compilation and renderer startup, but it cannot prove desktop attachment, multi-monitor behavior, or real media playback on target hardware. Nothing here is "production ready" until the release checklist passes on real Windows hardware.

## Spec and analysis

- Full build spec: `~/workspace/user/files/WINDOWS_LIVE_WALLPAPER_BUILD_SPEC.md`
- Complete analysis, including research verification: `docs/ANALYSIS.md`
- Current status, honestly marked: `docs/IMPLEMENTATION_STATUS.md`

## Start local development from the zip

1. Extract the zip on a Windows PC (Visual Studio 2026, .NET 10 SDK, Windows SDK, CMake, Node 20+).
2. Run:
```powershell
.\scripts\bootstrap-windows.ps1
.\scripts\build-desktop.ps1
.\scripts\test-all.ps1
```
3. Launch the WinUI app, choose a free item in Discover, download it, then preview or apply it from My wallpapers.
4. Backend (optional, staging only):
```powershell
cd src\backend\api
npm ci
npm run typecheck
npm test
```
The deployed staging and production Workers have D1 and published-R2 bindings. Production currently contains five owned, free Loopdesk H.264 MP4 wallpapers. The desktop uses the production `workers.dev` API endpoint: select a free item in Discover, download it with SHA-256/size verification, then apply it from My wallpapers. The app has no editable catalog URL and does not scan arbitrary local media folders.

## What is complete, and what is not

Complete in code: WinUI cloud catalog and protected downloaded-wallpaper library, native primary-display renderer, integrity-checked Cloudflare downloads, and Workers API publishing for owned free MP4 files.

Not implemented: multi-monitor assignment, wallpaper persistence after app exit, power/session pause integration, premium accounts/entitlements, interactive wallpapers, playlists, and a signed direct installer.
