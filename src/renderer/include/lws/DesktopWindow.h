#pragma once

// Per-monitor wallpaper window (M1). The window is created as a plain
// child-style window and is only reparented by DesktopShellAdapter after
// safety checks pass. If attachment fails, the window is destroyed and
// the desktop is left untouched — never shown topmost as a fake wallpaper.

#include <windows.h>

namespace lws {

class DesktopWindow {
public:
    DesktopWindow() = default;
    ~DesktopWindow();

    DesktopWindow(const DesktopWindow&) = delete;
    DesktopWindow& operator=(const DesktopWindow&) = delete;

    bool Create(HMONITOR monitor, int width, int height);
    void Destroy();
    HWND Handle() const { return hwnd_; }
    HMONITOR Monitor() const { return monitor_; }

private:
    HWND hwnd_ = nullptr;
    HMONITOR monitor_ = nullptr;
};

}  // namespace lws
