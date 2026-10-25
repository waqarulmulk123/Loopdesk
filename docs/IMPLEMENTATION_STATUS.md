# Implementation status — honest, by milestone

Updated: 2026-10-25 — five owned free wallpapers published to Cloudflare; desktop online download/apply flow and Windows CI verified

The source is on GitHub and the Cloudflare catalog infrastructure is deployed. GitHub Actions has passed the Windows build/startup job. This is not a desktop-product release: physical Windows acceptance tests, signed packaging, media processing, and entitlement integrations remain required.

| Milestone | Status | Evidence / gap |
|---|---|---|
| M0 — Environment, source, feasibility | CI verified; physical validation pending | GitHub Actions builds the .NET components and native renderer, runs the .NET harness, and starts the renderer. |
| M1 — Minimal real desktop video wallpaper | Partially verified on Windows | On 2026-10-25, the Release renderer attached to Explorer's WorkerW host, decoded/presented the bundled H.264 sample (IPC frames 142 → 269), accepted shutdown, and exited 0 on Windows 10 x64 / Intel HD Graphics 620. `scripts/test-renderer-video-smoke.ps1` reproduces that test. A human visual icon-interaction check is still required before the M1 gate is met. |
| M2 — Reliable engine, multi-monitor | Code written, NOT tested on Windows | Display model (device, bounds, DPI, primary), IPC handler wired into the render loop, bootstrap/sample/test scripts. Explorer-restart re-attachment and device-loss recreation are coded as re-attach/recreate paths, not yet proven on Windows. Gate not met yet. |
| M3 — Full offline Windows application | CI verified; physical acceptance pending | WinUI 3 app, MVVM view model, renderer supervisor, IPC client, settings, download manager, cache rules and SQLite schema build in the Windows workflow. Discover downloads a selected free online MP4, verifies its SHA-256/size, caches it, and applies it through the renderer. Physical acceptance remains required. |
| M4 — Free Cloudflare backend | Deployed and verified | Production Worker, D1 and R2 publish five owned free H.264 MP4s. Catalog listing, download authorization and a real 1 KiB `206` range response were verified on 2026-10-25. No paid Cloudflare Container, Queue or custom domain is required. |
| M5 — Production UX, security, install, tests | CI verified; signing/physical install pending | Backend, processor and Windows desktop GitHub workflows passed for commit `43b4427`. A signed installer and clean-machine install evidence still require publisher-owned signing credentials and a physical test machine. |
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
- Account, payment and premium-entitlement workflows remain deliberately disabled; premium media is never served without an entitlement provider.
- No installer has been built or signed.
- No payment/OIDC entitlement system or admin authorization policy is deployed.
- No custom Cloudflare zone/domain is attached for public media delivery.

## Blockers / limitations

- The primary renderer attachment/playback smoke test has passed on one Windows 10 / Intel HD Graphics 620 system. Display-change, Explorer-restart, multi-monitor, and visual icon-interaction acceptance still require dedicated physical tests.
- Code signing requires a certificate/account owned by the release publisher.
- No automatic cloud transcoding is available on the free tier; creators prepare video locally with FFmpeg before upload.
