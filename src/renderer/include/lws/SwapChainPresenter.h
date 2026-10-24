#pragma once

// DXGI swap-chain presenter for one wallpaper window (M1).
// The Media Foundation player transfers frames directly into the
// back buffer; this class only owns the swap chain and presents it.

#include <d3d11.h>
#include <dxgi.h>
#include <windows.h>
#include <wrl/client.h>

namespace lws {

class SwapChainPresenter {
public:
    bool Create(ID3D11Device* device, HWND hwnd, int width, int height);
    ID3D11Texture2D* BackBuffer();
    void Present();
    void Destroy();

private:
    Microsoft::WRL::ComPtr<IDXGISwapChain> swapChain_;
    Microsoft::WRL::ComPtr<ID3D11Texture2D> backBuffer_;
};

}  // namespace lws
