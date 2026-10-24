# Performance — well-performing by design

## Renderer

- Direct3D 11, hardware device first, WARP fallback only if hardware is unavailable.
- DXGI swap chain with `Present(1, 0)` — VSync, no tearing, no busy-loop.
- Media Foundation hardware decode first; frame-server mode, loop by seeking to 0 on ended.
- One renderer process per user session, multiple monitor windows — not one process per monitor.
- Pause policy (PlaybackPolicy): pause on fullscreen-on-monitor, lock, display-off, Battery Saver, remote desktop. Paused monitors present nothing and decode nothing.

## App

- No polling: pages refresh on demand, tray icon is event-driven, IPC is request/response.
- Preview is opt-in; the app does not decode video at launch.
- Downloads stream to disk (no whole-file buffering), with Range resume and SHA-256 verification on completion only.
- Cache LRU evicts only unpinned/inactive/non-imported files, so a favourite wallpaper is never evicted under you.

## What to measure on Windows (before any performance claim)

- GPU utilisation and VRAM at idle, 720p, and 4K, single and dual monitor.
- CPU at idle, playing, and paused.
- Memory/handle growth over a 24-hour loop (see TEST_PLAN AT-15).
- Battery impact on a laptop, with and without LowPowerMode / Battery Saver pause.
- Frame drops on Intel, AMD, and NVIDIA GPUs, at 720p and 4K.

Record OS build, GPU + driver, display config, and codec for every measurement. Do not claim "well performing" from code alone — claim it from those measurements.
