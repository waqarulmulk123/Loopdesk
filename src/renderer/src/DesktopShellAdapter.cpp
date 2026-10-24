#include "lws/DesktopShellAdapter.h"

#include <iostream>

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

DesktopAttachResult DesktopShellAdapter::TryAttach(HWND wallpaperHwnd, HMONITOR /*monitor*/) {
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

    HWND previousParent = SetParent(wallpaperHwnd, state.hostWorker);
    (void)previousParent;
    if (GetParent(wallpaperHwnd) != state.hostWorker) {
        result.diagnostics = L"SetParent to WorkerW failed; desktop left untouched.";
        return result;
    }

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
