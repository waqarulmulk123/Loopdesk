# Complete analysis — Windows Live Wallpaper

Date: 2026-10-24
Sources: the user's assessment in chat, plus `WINDOWS_LIVE_WALLPAPER_BUILD_SPEC.md` (revision 1.0, research checkpoint 2026-10-24), checked against Microsoft and Cloudflare documentation on 2026-10-24.

## Verdict

The combined recommendation is technically strong and broadly suitable:

**WinUI 3 + .NET 10 + C++ + Direct3D 11 + Media Foundation/libmpv + Cloudflare R2 + Workers + PostgreSQL.**

There is no good reason to replace this foundation with Electron, Tauri, a fully Rust architecture, or D3D12 just to look more advanced. The competitive advantage will come from renderer quality, power management, reliable Windows integration, fast delivery, and polished UX — not from adding more frameworks.

## What the earlier research got right

- WinUI 3 + C# + .NET 10 LTS for the modern Windows interface.
- Native C++ + Direct3D 11 for high-performance wallpaper rendering.
- Media Foundation and libmpv as alternative video playback backends.
- WebView2 for HTML/WebGL interactive wallpapers.
- Cloudflare R2 + Workers + CDN for content distribution.
- Download once, play locally — never continuously stream a wallpaper.
- Windows 7/8 compatibility as a separate engineering problem.
- Prioritising Explorer integration, GPU efficiency, multi-monitor handling, and automatic pause behaviour.

## Important corrections made before this becomes the final architecture

| Earlier recommendation | Final recommendation |
|---|---|
| One renderer process per monitor | One renderer process per user session, managing multiple monitor windows; isolate further only if benchmarks justify it |
| D1 database | D1 for a simple catalog only; PostgreSQL via Hyperdrive for commercial requirements |
| Native C++ or Rust engine | Prefer C++ initially for established Win32, COM and Direct3D integration |
| Media Foundation and mpv | Implement a common playback interface, benchmark both, then choose defaults |
| D3D11/D3D12 shader backends | D3D11 first; add D3D12 only for a proven requirement |
| Separate engine auto-updates | Coordinated, compatibility-checked updates — never independently replace arbitrary binaries |
| Short-lived signed R2 URLs | Distinguish S3 presigned URLs from authenticated custom-domain CDN delivery |
| Support all Windows versions | Modern product + separately tested legacy edition |

The biggest correction: more processes, frameworks and technologies do not automatically improve scalability or performance. A well-designed native renderer with one process, multiple monitor targets, and dedicated decode/render threads may use fewer resources than spawning a complete media engine per display.

## Five details that needed clarification

### A. Windows 11 24H2 desktop integration

Windows 11 24H2 introduced serious compatibility problems for wallpaper apps, acknowledged by Microsoft and documented by Lively. But the layered-child / WorkerW technique is **not** a guaranteed, officially supported desktop-wallpaper API.

Fix: build a version-adaptive `DesktopShellAdapter` with runtime detection, multiple attachment strategies, Explorer-restart recovery, and a safe static-wallpaper fallback. Never hardcode one approach for all Windows versions, and never render a topmost fullscreen imitation if attachment fails.

### B. Cloudflare R2 signed URLs

This matters if premium wallpapers are sold. Cloudflare's S3-compatible presigned URLs do **not** work through R2 custom domains — they work through the R2 S3 API endpoint.

- Public wallpapers: custom CDN domain backed by R2.
- Premium wallpapers: private storage with short-lived authorized downloads, or a properly implemented edge authorization layer. CDN caching must never bypass premium access checks.
- Also note the documented 512 MB per-file cache limit on ordinary Cloudflare Free, Pro and Business plans, which matters for very large wallpaper videos.

### C. Cloudflare D1 vs PostgreSQL

Both can work, but their scaling models differ. D1 currently has a 10 GB maximum per paid-plan database, and each individual D1 database processes queries serially.

- Simple wallpaper catalog: D1 is attractive.
- Commercial platform with user accounts, creator submissions, purchases, reporting and complex relationships: managed PostgreSQL with Hyperdrive from the beginning.

### D. Code signing

Signing is important, but it does not guarantee Windows SmartScreen warnings will never appear. Microsoft's signing service is now called Azure Artifact Signing, formerly Trusted Signing. Availability is geographically restricted, so eligibility must be checked before depending on it.

### E. Latest stable development stack

Microsoft's documented current stable Windows App SDK release is **2.5.1, released 16 September 2026**. Use that stable release, not experimental versions, for a production application.

## Research verification (2026-10-24)

- Windows App SDK 2.5.1 stable, 16 Sep 2026, and Windows App SDK targets Windows 10 version 1809 (build 17763) or later — confirmed in Microsoft Learn downloads / system-requirements pages.
- R2 presigned URLs work with the S3 API domain (`<ACCOUNT_ID>.r2.cloudflarestorage.com`) and cannot be used with custom domains — confirmed in Cloudflare R2 presigned-URL docs.
- D1 maximum database size 10 GB (Workers Paid) / 500 MB (Free), and each D1 database is single-threaded / processes queries one at a time — confirmed in Cloudflare D1 limits docs.
- .NET 10 does not support Windows 7/8.1, per the spec and Microsoft's OS support documentation — hence the separate legacy edition decision.

## Finalized stack

| Component | Final choice |
|---|---|
| Windows UI | WinUI 3, XAML |
| Application language | C# 14 |
| Application runtime | .NET 10 LTS |
| Windows App SDK | 2.5.1 stable |
| Native engine language | C++20, selectively C++23 |
| Native APIs | Win32, COM |
| Graphics | Direct3D 11, DXGI |
| Main video playback | Media Foundation |
| Alternative video playback | libmpv |
| Interactive wallpapers | WebView2 |
| Interprocess communication | Versioned named pipes |
| Local database | SQLite |
| Local caching | Filesystem + SHA-256 validation |
| Cloud object storage | Cloudflare R2 Standard |
| Cloud delivery | Cloudflare CDN |
| Backend API | Workers, TypeScript, Hono |
| Cloud database | PostgreSQL through Hyperdrive |
| Background processing | Queues + FFmpeg processing workers |
| Authentication | OAuth/OIDC-based identity solution |
| Observability | Structured logs, OpenTelemetry, crash diagnostics |
| Automated testing | Native unit tests, .NET tests, Windows integration tests |
| CI/CD | GitHub Actions |
| Distribution | Microsoft Store MSIX + signed direct installer |
| Legacy edition | Separate C++ Win32 application |

## What to build first

Do **not** begin with the marketplace interface or the Cloudflare infrastructure. Build a native wallpaper engine prototype that demonstrates:

1. Hardware-decoded 4K video playing behind desktop icons without disrupting normal Windows functionality.
2. Two or more monitors, including independent wallpaper selection.
3. Efficient pause when a game is fullscreen, the desktop is hidden, or the system enters a power-saving state.
4. Reliable recovery when Explorer restarts, displays change, or the decoder encounters a recoverable failure.
5. Long-running tests across Windows versions and Intel, AMD and NVIDIA GPUs.

Only once those are stable, integrate WinUI 3 and then Cloudflare. A beautiful UI and an excellent cloud backend cannot compensate for an engine that crashes Explorer, stutters, or consumes excessive resources.

## Risks that remain open

- Undocumented `Progman` / `WorkerW` / `0x052C` attachment can break with Windows updates — per-build testing is mandatory.
- Media Foundation decoder availability depends on OS version, edition, driver and codec — do not promise universal HEVC/AV1 decode.
- Bundled FFmpeg/mpv and codecs need a formal licence, build-option and patent evaluation.
- No claim of identical operation on Windows 7, 8, 10 and 11 from one modern executable.
- This Linux workspace cannot build or run WinUI/native Windows code — Windows CI and physical testing are required before release.
