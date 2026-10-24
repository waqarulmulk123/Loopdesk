#pragma once

// Smart-pause policy (spec section 7.4). A paused wallpaper must not
// continue decoding unseen frames. Rules are evaluated per monitor:
// fullscreen foreground app on that monitor, session lock, display off,
// Battery Saver, and remote-desktop sessions all pause by default.
// Manual override is handled by the supervisor, not by this class.

#include <windows.h>

namespace lws {

struct PausePolicy {
    bool pauseOnFullscreen = true;
    bool pauseOnLock = true;
    bool pauseOnDisplayOff = true;
    bool pauseOnBatterySaver = true;
    bool pauseOnRemoteDesktop = true;
};

class PlaybackPolicy {
public:
    explicit PlaybackPolicy(PausePolicy policy) : policy_(policy) {}

    // Returns true if playback on the given monitor should be paused now.
    // sessionLocked / displayOff / batterySaver / remoteDesktop are supplied
    // by the renderer's session/power observers; fullscreen is probed here.
    bool ShouldPause(HMONITOR monitor, bool sessionLocked, bool displayOff,
                     bool batterySaver, bool remoteDesktop) const;

private:
    bool IsFullscreenOnMonitor(HMONITOR monitor) const;
    PausePolicy policy_;
};

}  // namespace lws
