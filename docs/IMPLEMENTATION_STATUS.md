# Implementation status — honest, by milestone

Updated: 2026-10-25 — Cloudflare catalog infrastructure deployed; desktop release validation pending

The source is on GitHub and the Cloudflare catalog infrastructure is deployed. GitHub Actions has passed the Windows build/startup job. This is not a desktop-product release: physical Windows acceptance tests, signed packaging, media processing, and entitlement integrations remain required.

| Milestone | Status | Evidence / gap |
|---|---|---|
| M0 — Environment, source, feasibility | CI verified; physical validation pending | GitHub Actions builds the .NET components and native renderer, runs the .NET harness, and starts the renderer. |
| M1 — Minimal real desktop video wallpaper | Code written, NOT tested on Windows | D3D11 device, display enumeration, pause policy, per-monitor window, swap-chain presenter, Media Foundation frame-server player, WorkerW shell adapter, named-pipe IPC server. Gate (visible video behind icons) not met yet. |
| M2 — Reliable engine, multi-monitor | Code written, NOT tested on Windows | Display model (device, bounds, DPI, primary), IPC handler wired into the render loop, bootstrap/sample/test scripts. Explorer-restart re-attachment and device-loss recreation are coded as re-attach/recreate paths, not yet proven on Windows. Gate not met yet. |
| M3 — Full offline Windows application | Code written, NOT tested on Windows | WinUI 3 app, MVVM view model, renderer supervisor, IPC client, settings, download manager, cache rules, SQLite schema. Tray, library/displays views and MSIX packaging project are notes/skeleton only. Gate not met yet. |
| M4 — Real Cloudflare backend | API deployed; processor requires Workers Paid | Staging and production API Workers, D1 databases, R2 buckets, ingest/dead-letter Queues, Queue consumers, token-protected admin workflow, and Workers.dev media delivery are deployed. The FFmpeg processor image builds and runs locally, but Cloudflare Container instances cannot run until this account has Workers Paid access. No custom CDN domain, licensed catalog content, or premium entitlement provider is deployed. |
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

- The Cloudflare Container processor source and Docker image are verified locally, but Cloudflare Container execution is blocked by the account's missing Workers Paid entitlement; queued media cannot become published variants until that plan is enabled.
- The desktop Discover screen consumes the configured HTTPS API catalog; applying an online item and account/premium workflows remain incomplete.
- No licensed media has been ingested or published.
- No installer has been built or signed.
- No payment/OIDC entitlement system or admin authorization policy is deployed.
- No custom Cloudflare zone/domain is attached for public media delivery.

## Blockers / limitations

- A real Windows machine with supported graphics/display hardware is required for desktop-attachment, playback, display-change, and Explorer-restart acceptance tests.
- Code signing requires a certificate/account owned by the release publisher.
- Container/external-runner access is required for FFmpeg processing; Workers cannot execute FFmpeg in an HTTP request.
