#pragma once

// Multi-monitor model (M2, spec section 7.3).
// Each display keeps a stable-ish key (device name), bounds, DPI and
// refresh rate, so assignments survive disconnect/reconnect and
// re-enumeration in a new order. Span mode is built from these records
// in the supervisor; the renderer itself renders one window per
// assigned display, or one virtual region for span.

#include <string>
#include <vector>
#include <windows.h>

namespace lws {

struct DisplayInfo {
    HMONITOR monitor = nullptr;
    std::wstring deviceName;
    int x = 0;
    int y = 0;
    int width = 0;
    int height = 0;
    UINT dpiX = 96;
    UINT dpiY = 96;
    bool primary = false;
};

std::vector<DisplayInfo> EnumerateDisplays();
int ListDisplays();  // M0 helper: prints displays, returns count

}  // namespace lws
