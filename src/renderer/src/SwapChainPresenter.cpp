#include "lws/SwapChainPresenter.h"

namespace lws {

bool SwapChainPresenter::Create(ID3D11Device* device, HWND hwnd, int width, int height) {
    Destroy();
    if (!device || !hwnd || !IsWindow(hwnd) || width <= 0 || height <= 0) return false;

    Microsoft::WRL::ComPtr<IDXGIDevice> dxgiDevice;
    if (FAILED(device->QueryInterface(IID_PPV_ARGS(dxgiDevice.GetAddressOf())))) return false;
    Microsoft::WRL::ComPtr<IDXGIAdapter> adapter;
    if (FAILED(dxgiDevice->GetAdapter(adapter.GetAddressOf()))) return false;
    Microsoft::WRL::ComPtr<IDXGIFactory> factory;
    if (FAILED(adapter->GetParent(IID_PPV_ARGS(factory.GetAddressOf())))) return false;

    DXGI_SWAP_CHAIN_DESC desc{};
    desc.BufferCount = 2;
    desc.BufferDesc.Width = static_cast<UINT>(width);
    desc.BufferDesc.Height = static_cast<UINT>(height);
    desc.BufferDesc.Format = DXGI_FORMAT_B8G8R8A8_UNORM;
    desc.BufferDesc.RefreshRate.Numerator = 60;
    desc.BufferDesc.RefreshRate.Denominator = 1;
    desc.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
    desc.OutputWindow = hwnd;
    desc.SampleDesc.Count = 1;
    desc.Windowed = TRUE;
    desc.SwapEffect = DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL;

    if (FAILED(factory->CreateSwapChain(device, &desc, swapChain_.GetAddressOf()))) return false;
    if (FAILED(swapChain_->GetBuffer(0, IID_PPV_ARGS(backBuffer_.GetAddressOf())))) {
        Destroy();
        return false;
    }
    return true;
}

ID3D11Texture2D* SwapChainPresenter::BackBuffer() { return backBuffer_.Get(); }

void SwapChainPresenter::Present() {
    if (swapChain_) {
        // Present with vsync (1). The M2 policy layer may switch to 0
        // for high-refresh displays after measurement.
        swapChain_->Present(1, 0);
    }
}

void SwapChainPresenter::Destroy() {
    backBuffer_.Reset();
    swapChain_.Reset();
}

}  // namespace lws
