#include "lws/PlaybackPolicy.h"

namespace lws {

bool PlaybackPolicy::ShouldPause(HMONITOR monitor, bool sessionLocked, bool displayOff,
                                 bool batterySaver, bool remoteDesktop) const {
    if (policy_.pauseOnLock && sessionLocked) return true;
    if (policy_.pauseOnDisplayOff && displayOff) return true;
    if (policy_.pauseOnBatterySaver && batterySaver) return true;
    if (policy_.pauseOnRemoteDesktop && remoteDesktop) return true;
    if (policy_.pauseOnFullscreen && IsFullscreenOnMonitor(monitor)) return true;
    return false;
}

bool PlaybackPolicy::IsFullscreenOnMonitor(HMONITOR monitor) const {
    HWND foreground = GetForegroundWindow();
    if (!foreground || !monitor) return false;
    if (MonitorFromWindow(foreground, MONITOR_DEFAULTTONULL) != monitor) return false;

    // A fullscreen app covers the whole monitor and is not a normal
    // resizable window. Always-on-top utility windows are not counted:
    // they do not cover the full monitor rectangle.
    RECT windowRect{};
    RECT monitorRect{};
    MONITORINFO info{};
    info.cbSize = sizeof(info);
    if (!GetWindowRect(foreground, &windowRect)) return false;
    if (!GetMonitorInfoW(monitor, &info)) return false;
    monitorRect = info.rcMonitor;
    return windowRect.left <= monitorRect.left && windowRect.top <= monitorRect.top &&
           windowRect.right >= monitorRect.right && windowRect.bottom >= monitorRect.bottom;
}

}  // namespace lws
