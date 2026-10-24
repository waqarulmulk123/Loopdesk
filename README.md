# Live Wallpaper Studio

A native Windows live-wallpaper application. Working product name only — rename before release.

## What it is

- WinUI 3 desktop app for browsing, importing and applying wallpapers
- Native C++ renderer that plays video **behind desktop icons**, with multi-monitor support
- Offline-first: imported and downloaded wallpapers play without internet
- Optional Cloudflare-backed catalog: Workers + PostgreSQL via Hyperdrive + R2 + CDN

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
| Catalog database | Managed PostgreSQL via Hyperdrive |
| Storage / delivery | Cloudflare R2 Standard, custom-domain CDN for public assets |
| Ingest | Cloudflare Queues + FFmpeg processing job (never inside a Worker request) |
| CI / distribution | GitHub Actions, MSIX for Store, signed direct installer |

## Key architecture decisions

- **One renderer process per user session**, not one per monitor. Start there; add isolation only if benchmarks justify it.
- **D3D11 first.** Add D3D12 only for a proven, measured requirement.
- **Desktop attachment is undocumented and version-sensitive**, especially Windows 11 24H2. Use a version-adaptive `DesktopShellAdapter` with runtime detection, Explorer-restart recovery, and a safe static-wallpaper fallback. Never cover icons or render a topmost fullscreen imitation.
- **Premium delivery is not "signed R2 URL on a custom domain".** R2 S3 presigned URLs work through the S3 API endpoint, not custom domains. Public assets go on a custom CDN domain; premium assets use private R2 with short-lived authorized delivery and cache keys that respect authorization.
- **D1 is not the commercial catalog database.** D1 is fine for a simple catalog (10 GB max on Workers Paid, serial query processing per database). For accounts, creators, purchases and complex relationships, use PostgreSQL via Hyperdrive from the start.
- **Legacy Windows 7/8 is a separate edition**, not v1. The main product targets Windows 10 1809+ and Windows 11. .NET 10 does not support Windows 7/8.1.
- **Signing does not guarantee no SmartScreen prompt.** Azure Artifact Signing (formerly Trusted Signing) eligibility is geographically restricted — check before depending on it.

## Build order (spec milestones)

M0 environment/feasibility → M1 minimal real desktop video wallpaper → M2 reliable engine + multi-monitor → M3 full offline app → M4 real Cloudflare backend → M5 production UX, security, install and tests → M6 P1 extensions.

## Important limitation in this workspace

This workspace runs on Linux, so WinUI 3 and the native Windows renderer cannot be built or run here. Per the build spec §0 rule 12, source and backend tests are developed here, and Windows CI / a physical Windows machine must verify every milestone before any release claim. Nothing here is "production ready" until the §21 release checklist passes on real Windows hardware.

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
3. Put a local H.264 MP4 in `%LOCALAPPDATA%\LiveWallpaperStudio\media`, launch the WinUI app, select it in the Local library, and press Apply wallpaper.
4. Backend (optional, staging only):
```powershell
cd src\backend\api
npm ci
npm run typecheck
npm test
```
Configure `DATABASE_URL` / Hyperdrive, R2 buckets and Queues from `.env.example` before expecting a real catalog — without them the API honestly returns an empty catalog.

## What is complete, and what is not

Complete in code for local development: WinUI app (home, local library, displays, settings service, tray service, renderer supervisor, IPC client), native renderer (D3D11, Media Foundation, shell adapter, IPC server, pause policy), .NET Core services (settings, download manager, cache, IPC validation, SQLite schema), Workers API (real PostgreSQL catalog queries, admin publish/unpublish, public authorised downloads, premium refusal without entitlement), Queues processing contract, FFmpeg job, MSIX manifest template, CI workflow, docs and test plan.

Still P1 / not implemented: WebView2 interactive wallpapers, playlists, premium sync, account favourites, and a signed installer. Still not verified: every Windows build, desktop-attachment, multi-monitor and physical acceptance test in `docs/TEST_PLAN.md` — run those on real Windows before any release claim.
