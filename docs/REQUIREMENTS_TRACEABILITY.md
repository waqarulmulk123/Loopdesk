# Requirements traceability

Status definitions: **implemented** means source is present and automated checks cover only the listed scope; **verified** requires execution evidence; **blocked** requires an external account, credential, or physical Windows hardware; **not-started** means no release implementation exists.

| ID | Status | Code / evidence | Remaining acceptance evidence |
|---|---|---|---|
| APP-001 | implemented | `src/desktop/LiveWallpaperStudio.App/` | Launch on a physical Windows target. |
| APP-002 | implemented | `assets/wallpapers/`, library services | First-launch offline test. |
| APP-003 | implemented | library view models and core cache services | Physical file-picker/import test. |
| APP-004 | partially verified | WorkerW adapter passed `scripts/test-renderer-video-smoke.ps1` on Windows 10 / Intel HD Graphics 620 | Human visual validation that desktop icons remain usable. |
| APP-005 | partially verified | Renderer presented advancing frames from the bundled real video (142 → 269) | Loop observation over a complete media duration. |
| APP-006 | verified | Media Foundation decoded the bundled H.264 sample in the Windows renderer smoke test | Hardware compatibility matrix. |
| APP-007 | implemented | `RenderSupervisor.cs`, IPC server | Renderer-kill recovery test. |
| APP-008 | implemented | `DisplayManager.cpp`, displays view | Dual-display hot-plug test. |
| APP-009 | implemented | presenter, protocol, display services | Per-monitor persistence test. |
| APP-010 | implemented | playback policy and tray services | Physical pause/volume test. |
| APP-011 | implemented | `PlaybackPolicy.cpp` | Lock, fullscreen and power-state test. |
| APP-012 | implemented | tray/settings services | Login/startup lifecycle test. |
| APP-013 | implemented | SQLite schema and core services | Reboot/crash persistence test. |
| APP-014 | implemented | `DownloadManager.cs` | Range/ETag/cancellation server test. |
| APP-015 | implemented | cache/download services | Corrupt-download and dedupe test. |
| APP-016 | implemented | local cache and renderer path | Disconnected physical playback test. |
| APP-017 | implemented | `OnlineCatalogService.cs`, Discover view | Real catalog item E2E test. |
| APP-018 | implemented | deployed Worker, D1, R2, `src/backend/api/` | Catalog fixture must be published. |
| APP-019 | blocked | direct-publish API is implemented | Configure `ADMIN_API_TOKEN`, publish licensed media, then run E2E. |
| APP-020 | implemented | shell adapter, device/display paths | Explorer restart and device-reset tests. |
| APP-021 | implemented | app and renderer diagnostics paths | Windows error-path test. |
| APP-022 | implemented | settings/UI projects | Accessibility screen-reader test. |
| APP-023 | implemented | MSIX manifest and packaging scripts | Unsigned installer and uninstall test. |
| APP-024 | implemented | diagnostics view and renderer diagnostics | Export inspection on Windows. |
| APP-025 | implemented | README, scripts, CI, test plan | Full physical acceptance matrix. |

The current release is **not verified for production**. The authoritative release gates and test outcomes are maintained in `IMPLEMENTATION_STATUS.md`, `TEST_PLAN.md`, and `RELEASE_CHECKLIST.md`.
