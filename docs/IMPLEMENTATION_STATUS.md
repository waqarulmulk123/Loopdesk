# Implementation status — honest, by milestone

Updated: 2026-10-24 — full development code complete for local start, Windows verification pending

Development code for M0–M5 is complete in this workspace and is
delivered as a zip for local Windows development. It is NOT verified
on Windows yet, and nothing here is production ready until the
Windows builds and the physical Windows acceptance tests in
docs/TEST_PLAN.md pass. GitHub push was stopped in favour of the zip:
the single-snapshot push is blocked by a 403 on git-tree creation,
and the workflow file also needs Workflows permission (see MEMORY.md).

| Milestone | Status | Evidence / gap |
|---|---|---|
| M0 — Environment, source, feasibility | Code written, NOT tested on Windows | Repo structure, README, analysis, IPC schema, .NET Core library, test harness, CMake renderer, Windows CI workflow. WinUI window, native exe and tests not yet run on Windows. |
| M1 — Minimal real desktop video wallpaper | Code written, NOT tested on Windows | D3D11 device, display enumeration, pause policy, per-monitor window, swap-chain presenter, Media Foundation frame-server player, WorkerW shell adapter, named-pipe IPC server. Gate (visible video behind icons) not met yet. |
| M2 — Reliable engine, multi-monitor | Code written, NOT tested on Windows | Display model (device, bounds, DPI, primary), IPC handler wired into the render loop, bootstrap/sample/test scripts. Explorer-restart re-attachment and device-loss recreation are coded as re-attach/recreate paths, not yet proven on Windows. Gate not met yet. |
| M3 — Full offline Windows application | Code written, NOT tested on Windows | WinUI 3 app, MVVM view model, renderer supervisor, IPC client, settings, download manager, cache rules, SQLite schema. Tray, library/displays views and MSIX packaging project are notes/skeleton only. Gate not met yet. |
| M4 — Real Cloudflare backend | Skeleton written, NOT deployed | Workers/Hono API skeleton, PostgreSQL migration 001, wrangler config, FFmpeg processing job. No buckets, Queues deployment, admin publish or premium authorisation deployed. Gate not met yet. |
| M5 — Production UX, security, install, tests | Docs written, NOT verified | Release checklist, test plan, API, security and deployment docs, packaging notes. No installer, signing workflow or release evidence yet. Gate not met yet. |
| M6 — P1 extensions | Not started | WebView2 interactive wallpapers, playlists, premium sync — documented as not implemented, not shown as enabled features. |

## What is in this workspace now

- `README.md`, `docs/ANALYSIS.md`, `docs/ARCHITECTURE.md`, `docs/API.md`, `docs/SECURITY.md`, `docs/DEPLOYMENT.md`, `docs/TEST_PLAN.md`, `docs/RELEASE_CHECKLIST.md`
- `src/renderer/` — native C++ renderer (CMake, D3D11, Media Foundation, shell adapter, IPC)
- `src/desktop/` — WinUI 3 app, .NET Core library, SQLite schema
- `src/backend/` — Workers/Hono API skeleton, PostgreSQL migration, FFmpeg processing job
- `src/shared/contracts/protocol.schema.json` — IPC protocol
- `tests/Core.Tests/` — .NET test harness
- `.github/workflows/desktop-build.yml` — Windows CI
- `scripts/` — bootstrap, build, test, sample-video, package, publish-backend

## What is NOT done

- No code in this complete set has been compiled or run on Windows yet.
- No Cloudflare resources have been created or configured, and no auth was requested for that in this step.
- No installer has been built or signed.
- No feature is claimed as working, and nothing here is production ready.

## Blockers / limitations

- Linux workspace: WinUI 3 / native Windows builds and desktop-attachment tests cannot run here (spec §0 rule 12).
- Windows CI (GitHub Actions, chosen by the user) or a physical Windows machine is required for verification.
- Target repository: `waqarulmulk123/Loopdesk` — confirmed by the user as the project repo; only README/docs/global.json/.gitignore are on Loopdesk main so far. The full code is pushed only after development is complete, per the user's instruction.
