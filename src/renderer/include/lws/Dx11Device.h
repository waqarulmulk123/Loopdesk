#pragma once

// D3D11 device wrapper for the wallpaper renderer (M1).
// Hardware first, WARP fallback only if no hardware device is available.
// Device-removed handling is explicit: callers must recreate the device
// and all device-dependent resources, never present from a lost device.

#include <d3d11.h>
#include <dxgi.h>
#include <string>
#include <wrl/client.h>

namespace lws {

class Dx11Device {
public:
    Dx11Device() = default;
    Dx11Device(const Dx11Device&) = delete;
    Dx11Device& operator=(const Dx11Device&) = delete;

    // Returns true on success. On failure, diagnostics() explains why.
    bool Create();
    bool IsLost() const;
    void Reset();

    ID3D11Device* Device() const { return device_.Get(); }
    ID3D11DeviceContext* Context() const { return context_.Get(); }
    IDXGIAdapter* Adapter() const { return adapter_.Get(); }
    const std::wstring& Diagnostics() const { return diagnostics_; }

private:
    Microsoft::WRL::ComPtr<ID3D11Device> device_;
    Microsoft::WRL::ComPtr<ID3D11DeviceContext> context_;
    Microsoft::WRL::ComPtr<IDXGIAdapter> adapter_;
    std::wstring diagnostics_;
};

}  // namespace lws
