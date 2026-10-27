# Test plan

## Automated (GitHub Actions Windows, desktop-build.yml)

- .NET Core library builds (Release, x64)
- WinUI app builds (Release, x64)
- .NET test harness runs: IPC validation, settings defaults, SHA-256 vector, cache LRU rules
- Native renderer builds with CMake/MSVC and runs its startup proof
- Renderer artifact is uploaded

## Manual / physical Windows (required before release)

- AT-01 first-run offline: the cloud catalog reports an unavailable state without exposing local paths; previously downloaded wallpapers remain playable
- AT-02 real wallpaper: video behind icons, icons interactive
- AT-03 normal Windows behaviour: Start, desktop right-click, icon drag/select, app windows
- AT-04 loop: 8-second sample, 16 seconds elapsed, still advancing, no sustained black screen
- AT-05 crash isolation: kill renderer, WinUI stays responsive, supervisor recovers or leaves a safe fallback
- AT-06 Explorer restart: re-attach or report unsupported, never cover icons
- AT-07 multi-monitor: different assets per display, removal does not corrupt state
- AT-08 smart pause: fullscreen app pauses that monitor, resumes afterwards
- AT-09 download integrity: SHA-256 matches, plays offline, bad checksum rejected
- AT-10 network interrupted: resume with Range/ETag or clean restart
- AT-11 persistence: favourites and protected-download metadata survive exit and relaunch
- AT-12 cloud catalog: discover a published free item → authorize → download → SHA-256 verify → play offline
- AT-13 security: malformed IPC and unauthorised download are rejected
- AT-14 install/uninstall: clean install, clean uninstall, protected cloud downloads are handled as documented
- AT-15 resource stability: 24-hour loop, no unbounded handle/memory/VRAM growth
- AT-16 unsupported shell: explanatory error, icons available, no fullscreen imitation

## Hardware matrix to record for every run

OS build, Explorer version, CPU architecture, GPU + driver, display
configuration (single/dual, mixed DPI, portrait/landscape), codec
(H.264 720p/1080p/4K), and the exact test result (PASS / FAIL / NOT RUN).

## Reproducible physical attachment proof

On a Windows desktop, first download any owned test wallpaper with Loopdesk.
Then use its local cache file as the test input (do not use a repository media
file):

```powershell
.\scripts\test-renderer-desktop-attachment.ps1 -VideoPath "$env:LOCALAPPDATA\Loopdesk\media\<asset>.mp4"
```

Add `-RestartExplorer` to perform the structural Explorer-restart check. It
briefly restarts the Windows shell and passes only when the renderer recreates
its wallpaper window under a non-icon-hosting `WorkerW` and frames continue.

The script fails unless the real renderer window is a child of a `WorkerW`
that does not host `SHELLDLL_DefView` (the desktop-icon view), then confirms
that decoded frames advance and that shutdown is clean. It is a structural
proof of behind-icon attachment; manual icon click/drag observation remains
the UI acceptance portion of AT-02.
