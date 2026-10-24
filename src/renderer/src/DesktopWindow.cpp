#include "lws/DesktopWindow.h"

namespace lws {

namespace {
const wchar_t kClassName[] = L"LwsWallpaperWindow";

LRESULT CALLBACK WndProc(HWND hwnd, UINT msg, WPARAM wparam, LPARAM lparam) {
    if (msg == WM_DESTROY) {
        PostQuitMessage(0);
        return 0;
    }
    return DefWindowProcW(hwnd, msg, wparam, lparam);
}

bool EnsureClass() {
    static bool registered = false;
    if (registered) return true;
    WNDCLASSEXW wc{};
    wc.cbSize = sizeof(wc);
    wc.lpfnWndProc = WndProc;
    wc.hInstance = GetModuleHandleW(nullptr);
    wc.lpszClassName = kClassName;
    if (!RegisterClassExW(&wc)) {
        return GetLastError() == ERROR_CLASS_ALREADY_EXISTS;
    }
    registered = true;
    return true;
}
}  // namespace

DesktopWindow::~DesktopWindow() { Destroy(); }

bool DesktopWindow::Create(HMONITOR monitor, int width, int height) {
    if (!EnsureClass()) return false;
    MONITORINFO info{};
    info.cbSize = sizeof(info);
    int x = 0, y = 0;
    if (monitor && GetMonitorInfoW(monitor, &info)) {
        x = info.rcMonitor.left;
        y = info.rcMonitor.top;
    }
    // WS_POPUP without WS_EX_TOPMOST: the shell adapter decides parenting.
    // The window must never be shown topmost on its own.
    hwnd_ = CreateWindowExW(
        0, kClassName, L"", WS_POPUP | WS_VISIBLE,
        x, y, width, height,
        nullptr, nullptr, GetModuleHandleW(nullptr), nullptr);
    if (!hwnd_) return false;
    monitor_ = monitor;
    return true;
}

void DesktopWindow::Destroy() {
    if (hwnd_) {
        DestroyWindow(hwnd_);
        hwnd_ = nullptr;
    }
    monitor_ = nullptr;
}

}  // namespace lws
