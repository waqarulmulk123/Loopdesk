# Requirements traceability

Status definitions: **implemented** is present in source and covered by the stated automated evidence. **verified** has runtime evidence. **manual** needs a physical Windows test. **not implemented** is intentionally not presented as a working feature.

| Area | Status | Evidence / remaining acceptance |
| --- | --- | --- |
| WinUI shell, onboarding, navigation | implemented | `src/desktop/LiveWallpaperStudio.App/`; build succeeds. Verify on physical Windows at supported DPI settings. |
| Cloud catalog, search, category filter | implemented | `OnlineCatalogService`, `DiscoverViewModel`, backend verification. Test catalog outage and production response on Windows. |
| Protected cloud download | implemented | HTTPS-only `DownloadManager`, SHA-256/size tests, DPAPI vault. Test download/apply end-to-end on Windows. |
| Local-drive scanning | intentionally absent | Library lists only Loopdesk-managed downloads; no arbitrary folders or paths are scanned or displayed. |
| User-selected MP4 import | not implemented | Decision D1 is recorded; no import UI or file picker is shipped. |
| Home catalog cards and greeting | implemented | `HomeViewModel` obtains cards from the cloud catalog and derives greeting from local time. |
| Favourites | implemented | `FavouritesService` local persistence. Offline metadata-rich card rendering remains pending. |
| Download queue, progress, cancel, cache limit | not implemented | Current download flow verifies completed files but has no DownloadCenter UI. |
| Per-display assignment / multi-monitor playback | not implemented | Displays page reports Win32 monitor count and primary resolution only. |
| Pause policy, mute, fit mode, frame cap | not implemented | Preferences persist locally but are not pushed into renderer behavior. |
| Renderer WorkerW attachment | partially verified | One Windows 10 single-monitor proof is recorded. Validate Explorer interaction, restart, device loss, and Windows 11 releases manually. |
| Tray/background lifetime / restore after restart | not implemented | `TrayService` is not instantiated and closing the app stops the renderer. |
| MSIX packaging | implemented | Packaging scripts and MSIX build exist. Signing and clean-machine install/uninstall remain manual release gates. |
| Premium identity, payments, entitlement | not implemented | Premium download remains refused until the owner selects an identity/entitlement provider. |
| Cloudflare API / catalog | verified | Worker, D1, R2 and production range download were previously verified; run backend verify before backend changes. |

The authoritative physical acceptance checklist is [MANUAL_TEST_CHECKLIST.md](MANUAL_TEST_CHECKLIST.md). The project is not production-verified until its applicable items are completed on physical Windows hardware.
