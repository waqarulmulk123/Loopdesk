#pragma once

// M0 shell adapter interface, from build spec section 6.
// This is the interface only. The M1 implementation must probe the real
// Progman / WorkerW hierarchy, validate safety, and fall back to a static
// wallpaper if no strategy passes. It must never cover desktop icons.

#include <string>
#include <windows.h>

namespace lws {

struct DesktopAttachResult {
    bool success = false;
    HWND hostWindow = nullptr;
    std::wstring strategyName;
    std::wstring diagnostics;
};

class IDesktopShellAdapter {
public:
    virtual ~IDesktopShellAdapter() = default;
    virtual DesktopAttachResult TryAttach(HWND wallpaperHwnd, HMONITOR monitor) = 0;
    virtual void Detach(HWND wallpaperHwnd) noexcept = 0;
    virtual void OnExplorerRestarted() = 0;
};

class DesktopShellAdapter final : public IDesktopShellAdapter {
public:
    DesktopAttachResult TryAttach(HWND wallpaperHwnd, HMONITOR monitor) override;
    void Detach(HWND wallpaperHwnd) noexcept override;
    void OnExplorerRestarted() override;
};

}  // namespace lws
