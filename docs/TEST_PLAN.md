# Test plan

## Automated (GitHub Actions Windows, desktop-build.yml)

- .NET Core library builds (Release, x64)
- WinUI app builds (Release, x64)
- .NET test harness runs: IPC validation, settings defaults, SHA-256 vector, cache LRU rules
- Native renderer builds with CMake/MSVC and runs its startup proof
- Renderer artifact is uploaded

## Manual / physical Windows (required before release)

- AT-01 first-run offline: bundled Loopdesk Originals are available and playable with no network connection
- AT-02 real wallpaper: video behind icons, icons interactive
- AT-03 normal Windows behaviour: Start, desktop right-click, icon drag/select, app windows
- AT-04 loop: 8-second sample, 16 seconds elapsed, still advancing, no sustained black screen
- AT-05 crash isolation: kill renderer, WinUI stays responsive, supervisor recovers or leaves a safe fallback
- AT-06 Explorer restart: re-attach or report unsupported, never cover icons
- AT-07 multi-monitor: different assets per display, removal does not corrupt state
- AT-08 smart pause: fullscreen app pauses that monitor, resumes afterwards
- AT-09 download integrity: SHA-256 matches, plays offline, bad checksum rejected
- AT-10 network interrupted: resume with Range/ETag or clean restart
- AT-11 persistence: settings, fit mode, favourites survive exit and relaunch
- AT-12 cloud catalog: discover a published free item → authorize → download → SHA-256 verify → play offline
- AT-13 security: malformed IPC and unauthorised download are rejected
- AT-14 install/uninstall: clean install, clean uninstall, imported originals untouched
- AT-15 resource stability: 24-hour loop, no unbounded handle/memory/VRAM growth
- AT-16 unsupported shell: explanatory error, icons available, no fullscreen imitation

## Hardware matrix to record for every run

OS build, Explorer version, CPU architecture, GPU + driver, display
configuration (single/dual, mixed DPI, portrait/landscape), codec
(H.264 720p/1080p/4K), and the exact test result (PASS / FAIL / NOT RUN).
