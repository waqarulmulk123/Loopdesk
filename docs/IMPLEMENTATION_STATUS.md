# Implementation status — honest, by milestone

Updated: 2026-10-26 — cloud-only private desktop flow and physical WorkerW playback verified

The source is on GitHub and the Cloudflare catalog infrastructure is deployed. GitHub Actions has passed the Windows build/startup job. This is not a desktop-product release: physical Windows acceptance tests, signed packaging, media processing, and entitlement integrations remain required.

| Milestone | Status | Evidence / gap |
|---|---|---|
| M0 — Environment, source, feasibility | CI verified; physical validation pending | GitHub Actions builds the .NET components and native renderer, runs the .NET harness, and starts the renderer. |
| M1 — Minimal real desktop video wallpaper | Physical playback verified | On 2026-10-26, the Release renderer attached its real `LwsWallpaperWindow` to a non-icon-hosting `WorkerW`, decoded/presented the owned H.264 sample (IPC frames 104 → 203), and shut down with exit 0 on Windows 10 Pro 19045 / Intel HD Graphics 620 (driver 31.0.101.2140). `scripts/test-renderer-desktop-attachment.ps1` reproduces this proof. |
| M2 — Reliable engine, multi-monitor | Single-monitor verified; multi-monitor pending | The renderer, IPC and display model are exercised on one 1920x1080 display. Explorer-restart, device-loss and true multi-monitor assignment require the corresponding physical display/event configurations. |
| M3 — Windows application | Build and renderer acceptance verified | Discover is cloud-only. It downloads a selected free Cloudflare MP4, verifies SHA-256/size, protects it per Windows user with DPAPI, and exposes preview/apply only from My wallpapers. The UI never scans or displays drive paths. |
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
- A test-signed QA installer exists under `artifacts/`; there is no production-signed installer release or reproducible production-installer source.
- No payment/OIDC entitlement system is deployed.
- No custom Cloudflare zone/domain is attached for public media delivery.

## Blockers / limitations

- The renderer attachment/playback proof passed on one Windows 10 / Intel HD Graphics 620 system with a single 1920x1080 monitor. Display-change, Explorer-restart, multi-monitor, visual icon click/drag, and install/uninstall acceptance still require their corresponding physical configurations.
- Code signing requires a certificate/account owned by the release publisher.
- No automatic cloud transcoding is available on the free tier; creators prepare video locally with FFmpeg before upload.
