#include "lws/Dx11Device.h"

#include <iterator>
#include <string>

namespace lws {

bool Dx11Device::Create() {
    UINT flags = D3D11_CREATE_DEVICE_BGRA_SUPPORT;
#if defined(_DEBUG)
    flags |= D3D11_CREATE_DEVICE_DEBUG;
#endif
    static const D3D_FEATURE_LEVEL levels[] = {
        D3D_FEATURE_LEVEL_11_1,
        D3D_FEATURE_LEVEL_11_0,
        D3D_FEATURE_LEVEL_10_1,
        D3D_FEATURE_LEVEL_10_0,
    };

    Microsoft::WRL::ComPtr<IDXGIFactory> factory;
    if (FAILED(CreateDXGIFactory(__uuidof(IDXGIFactory), reinterpret_cast<void**>(factory.GetAddressOf())))) {
        diagnostics_ = L"Failed to create DXGI factory.";
        return false;
    }

    // Try hardware adapters first, then WARP.
    for (UINT i = 0;; ++i) {
        Microsoft::WRL::ComPtr<IDXGIAdapter> adapter;
        if (factory->EnumAdapters(i, adapter.GetAddressOf()) == DXGI_ERROR_NOT_FOUND) {
            break;
        }
        D3D_FEATURE_LEVEL chosen = D3D_FEATURE_LEVEL_11_0;
        Microsoft::WRL::ComPtr<ID3D11Device> device;
        Microsoft::WRL::ComPtr<ID3D11DeviceContext> context;
        HRESULT hr = D3D11CreateDevice(
            adapter.Get(), D3D_DRIVER_TYPE_UNKNOWN, nullptr, flags,
            levels, static_cast<UINT>(std::size(levels)),
            D3D11_SDK_VERSION, device.GetAddressOf(), &chosen,
            context.GetAddressOf());
        if (SUCCEEDED(hr)) {
            device_ = device;
            context_ = context;
            adapter_ = adapter;
            diagnostics_ = L"Hardware D3D11 device created.";
            return true;
        }
    }

    D3D_FEATURE_LEVEL chosen = D3D_FEATURE_LEVEL_11_0;
    HRESULT hr = D3D11CreateDevice(
        nullptr, D3D_DRIVER_TYPE_WARP, nullptr, flags,
        levels, static_cast<UINT>(std::size(levels)),
        D3D11_SDK_VERSION, device_.GetAddressOf(), &chosen,
        context_.GetAddressOf());
    if (FAILED(hr)) {
        diagnostics_ = L"Failed to create a D3D11 device, including WARP fallback.";
        return false;
    }
    diagnostics_ = L"WARP D3D11 device created (no hardware device available).";
    return true;
}

bool Dx11Device::IsLost() const {
    if (!device_) {
        return true;
    }
    return device_->GetDeviceRemovedReason() != S_OK;
}

void Dx11Device::Reset() {
    context_.Reset();
    device_.Reset();
    adapter_.Reset();
    diagnostics_.clear();
}

}  // namespace lws
