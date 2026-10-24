#include "lws/DisplayManager.h"

#include <iostream>
#include <shellscalingapi.h>

namespace lws {

namespace {
BOOL CALLBACK CollectMonitor(HMONITOR monitor, HDC /*dc*/, LPRECT rect, LPARAM param) {
    auto* out = reinterpret_cast<std::vector<DisplayInfo>*>(param);
    MONITORINFOEXW info{};
    info.cbSize = sizeof(info);
    if (!GetMonitorInfoW(monitor, &info)) return TRUE;
    DisplayInfo display;
    display.monitor = monitor;
    display.deviceName = info.szDevice;
    display.x = rect->left;
    display.y = rect->top;
    display.width = rect->right - rect->left;
    display.height = rect->bottom - rect->top;
    display.primary = (info.dwFlags & MONITORINFOF_PRIMARY) != 0;
    UINT dpiX = 96, dpiY = 96;
    // GetDpiForMonitor is available on Windows 8.1+; fall back to 96.
    if (SUCCEEDED(GetDpiForMonitor(monitor, MDT_EFFECTIVE_DPI, &dpiX, &dpiY))) {
        display.dpiX = dpiX;
        display.dpiY = dpiY;
    }
    out->push_back(display);
    return TRUE;
}
}  // namespace

std::vector<DisplayInfo> EnumerateDisplays() {
    std::vector<DisplayInfo> displays;
    EnumDisplayMonitors(nullptr, nullptr,
        [](HMONITOR m, HDC dc, LPRECT r, LPARAM p) {
            return CollectMonitor(m, dc, r, p);
        },
        reinterpret_cast<LPARAM>(&displays));
    return displays;
}

int ListDisplays() {
    auto displays = EnumerateDisplays();
    for (const auto& d : displays) {
        std::wcout << L"Display: " << d.deviceName
                   << L" bounds=" << d.x << L"," << d.y << L" "
                   << d.width << L"x" << d.height
                   << L" dpi=" << d.dpiX << L"x" << d.dpiY
                   << (d.primary ? L" primary" : L"") << L"\n";
    }
    return static_cast<int>(displays.size());
}

}  // namespace lws
