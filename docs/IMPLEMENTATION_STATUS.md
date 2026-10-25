# Implementation status — honest, by milestone

Updated: 2026-10-25 — Cloudflare catalog infrastructure deployed; native renderer playback smoke-tested on Windows 10

The source is on GitHub and the Cloudflare catalog infrastructure is deployed. GitHub Actions has passed the Windows build/startup job. This is not a desktop-product release: physical Windows acceptance tests, signed packaging, media processing, and entitlement integrations remain required.

| Milestone | Status | Evidence / gap |
|---|---|---|
| M0 — Environment, source, feasibility | CI verified; physical validation pending | GitHub Actions builds the .NET components and native renderer, runs the .NET harness, and starts the renderer. |
| M1 — Minimal real desktop video wallpaper | Partially verified on Windows | On 2026-10-25, the Release renderer attached to Explorer's WorkerW host, decoded/presented the bundled H.264 sample (IPC frames 142 → 269), accepted shutdown, and exited 0 on Windows 10 x64 / Intel HD Graphics 620. `scripts/test-renderer-video-smoke.ps1` reproduces that test. A human visual icon-interaction check is still required before the M1 gate is met. |
| M2 — Reliable engine, multi-monitor | Code written, NOT tested on Windows | Display model (device, bounds, DPI, primary), IPC handler wired into the render loop, bootstrap/sample/test scripts. Explorer-restart re-attachment and device-loss recreation are coded as re-attach/recreate paths, not yet proven on Windows. Gate not met yet. |
| M3 — Full offline Windows application | Code written, NOT tested on Windows | WinUI 3 app, MVVM view model, renderer supervisor, IPC client, settings, download manager, cache rules, SQLite schema. Tray, library/displays views and MSIX packaging project are notes/skeleton only. Gate not met yet. |
| M4 — Free Cloudflare backend | Direct-publish API ready | Staging and production API Workers, D1 databases, published R2 buckets, token-protected direct MP4 publishing, and Workers.dev media delivery are configured. Media is prepared with local FFmpeg and uploaded as pre-encoded H.264 MP4; no paid Cloudflare Container or Queue consumer is required. No custom CDN domain, licensed catalog content, or premium entitlement provider is deployed. |
| M5 — Production UX, security, install, tests | Docs written, NOT verified | Release checklist, test plan, API, security and deployment docs, packaging notes. No installer, signing workflow or release evidence yet. Gate not met yet. |
| M6 — P1 extensions | Not started | WebView2 interactive wallpapers, playlists, premium sync — documented as not implemented, not shown as enabled features. |

## What is in this workspace now

- `README.md`, `docs/ANALYSIS.md`, `docs/ARCHITECTURE.md`, `docs/API.md`, `docs/SECURITY.md`, `docs/DEPLOYMENT.md`, `docs/TEST_PLAN.md`, `docs/RELEASE_CHECKLIST.md`
- `src/renderer/` — native C++ renderer (CMake, D3D11, Media Foundation, shell adapter, IPC)
- `src/desktop/` — WinUI 3 app, .NET Core library, SQLite schema
- `src/backend/` — Workers/Hono API, D1 migration, and FFmpeg processing contract
- `src/shared/contracts/protocol.schema.json` — IPC protocol
- `tests/Core.Tests/` — .NET test harness
- `.github/workflows/desktop-build.yml` — Windows CI
- `scripts/` — bootstrap, build, test, sample-video, package, publish-backend

## What is NOT done

- The free media workflow accepts only locally prepared H.264 MP4 files. Automatic cloud transcoding is deliberately disabled because it requires paid compute.
- The desktop Discover screen consumes the configured HTTPS API catalog; applying an online item and account/premium workflows remain incomplete.
- No licensed media has been ingested or published.
- No installer has been built or signed.
- No payment/OIDC entitlement system or admin authorization policy is deployed.
- No custom Cloudflare zone/domain is attached for public media delivery.

## Blockers / limitations

- The primary renderer attachment/playback smoke test has passed on one Windows 10 / Intel HD Graphics 620 system. Display-change, Explorer-restart, multi-monitor, and visual icon-interaction acceptance still require dedicated physical tests.
- Code signing requires a certificate/account owned by the release publisher.
- No automatic cloud transcoding is available on the free tier; creators prepare video locally with FFmpeg before upload.
