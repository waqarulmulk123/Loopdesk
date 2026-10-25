#include "lws/DesktopShellAdapter.h"

#include <iostream>
#include <sstream>

namespace lws {

namespace {

// Classic Progman / WorkerW probing. This is NOT a platform contract:
// it changed around Windows 11 24H2 and may change again, which is why
// every step is validated and failure returns "unsupported" instead of
// covering icons or showing a topmost fake wallpaper.
struct ProbeState {
    HWND progman = nullptr;
    HWND shellViewWorker = nullptr;  // WorkerW that hosts SHELLDLL_DefView
    HWND hostWorker = nullptr;       // WorkerW after the shell-view worker
    bool foundShellView = false;
};

BOOL CALLBACK EnumTopWindows(HWND hwnd, LPARAM param) {
    auto* state = reinterpret_cast<ProbeState*>(param);
    HWND defView = FindWindowExW(hwnd, nullptr, L"SHELLDLL_DefView", nullptr);
    if (defView) {
        state->shellViewWorker = hwnd;
        state->foundShellView = true;
        return TRUE;  // keep enumerating to find the next WorkerW
    }
    if (state->foundShellView && !state->hostWorker) {
        wchar_t className[64] = {};
        GetClassNameW(hwnd, className, 64);
        if (wcscmp(className, L"WorkerW") == 0) {
            state->hostWorker = hwnd;
            return FALSE;
        }
    }
    return TRUE;
}

}  // namespace

DesktopAttachResult DesktopShellAdapter::TryAttach(HWND wallpaperHwnd, HMONITOR monitor) {
    DesktopAttachResult result;
    if (!wallpaperHwnd || !IsWindow(wallpaperHwnd)) {
        result.diagnostics = L"Invalid wallpaper window handle.";
        return result;
    }

    HWND progman = FindWindowW(L"Progman", nullptr);
    if (!progman) {
        result.diagnostics = L"Progman window not found; shell attachment unsupported on this build.";
        return result;
    }

    // Ask Explorer to spawn the WorkerW layer. Undocumented message 0x052C.
    DWORD_PTR messageResult = 0;
    SendMessageTimeoutW(progman, 0x052C, 0, 0, SMTO_NORMAL, 1000, &messageResult);

    ProbeState state;
    state.progman = progman;
    EnumWindows(EnumTopWindows, reinterpret_cast<LPARAM>(&state));
    if (!state.hostWorker) {
        result.diagnostics = L"No suitable WorkerW host found; shell attachment unsupported on this build. Desktop left untouched.";
        return result;
    }

    // SetParent does not adjust WS_POPUP/WS_CHILD for us.  Explorer rejects
    // a popup as a WorkerW child on some Windows 10 builds, so make the
    // window a child before reparenting and restore its original style if
    // the operation cannot be verified.
    const LONG_PTR originalStyle = GetWindowLongPtrW(wallpaperHwnd, GWL_STYLE);
    SetWindowLongPtrW(wallpaperHwnd, GWL_STYLE,
                      (originalStyle & ~static_cast<LONG_PTR>(WS_POPUP)) | WS_CHILD);
    SetLastError(ERROR_SUCCESS);
    SetParent(wallpaperHwnd, state.hostWorker);
    const DWORD parentError = GetLastError();
    if (GetParent(wallpaperHwnd) != state.hostWorker) {
        SetWindowLongPtrW(wallpaperHwnd, GWL_STYLE, originalStyle);
        std::wstringstream diagnostics;
        diagnostics << L"SetParent to WorkerW failed (Win32 error " << parentError
                    << L"); desktop left untouched.";
        result.diagnostics = diagnostics.str();
        return result;
    }

    // Child coordinates are relative to WorkerW, while monitor coordinates
    // are screen-relative. Convert the selected monitor's physical bounds so
    // a scaled or offset desktop does not render only on the left side.
    MONITORINFO monitorInfo{};
    monitorInfo.cbSize = sizeof(monitorInfo);
    POINT hostOrigin{0, 0};
    ClientToScreen(state.hostWorker, &hostOrigin);
    if (!monitor || !GetMonitorInfoW(monitor, &monitorInfo)) {
        SetParent(wallpaperHwnd, nullptr);
        SetWindowLongPtrW(wallpaperHwnd, GWL_STYLE, originalStyle);
        result.diagnostics = L"Could not resolve monitor bounds after shell attachment.";
        return result;
    }
    const int width = monitorInfo.rcMonitor.right - monitorInfo.rcMonitor.left;
    const int height = monitorInfo.rcMonitor.bottom - monitorInfo.rcMonitor.top;
    const int x = monitorInfo.rcMonitor.left - hostOrigin.x;
    const int y = monitorInfo.rcMonitor.top - hostOrigin.y;
    SetWindowPos(wallpaperHwnd, HWND_BOTTOM, x, y, width, height,
                 SWP_FRAMECHANGED | SWP_NOACTIVATE);

    result.success = true;
    result.hostWindow = state.hostWorker;
    result.strategyName = L"WorkerW";
    result.diagnostics = L"Attached to WorkerW host behind desktop icons.";
    return result;
}

void DesktopShellAdapter::Detach(HWND wallpaperHwnd) noexcept {
    if (wallpaperHwnd && IsWindow(wallpaperHwnd)) {
        SetParent(wallpaperHwnd, nullptr);
    }
}

void DesktopShellAdapter::OnExplorerRestarted() {
    std::cout << "Explorer restart observed; caller must re-run TryAttach.\n";
}

}  // namespace lws
