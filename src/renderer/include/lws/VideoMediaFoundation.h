#pragma once

// Media Foundation Media Engine player, frame-server mode (M1).
// The engine decodes locally cached H.264 MP4 and transfers each new
// frame to a caller-supplied D3D11 texture (normally a swap-chain back
// buffer). It never streams, never renders on its own, and loops by
// seeking to 0 on MF_MEDIA_ENGINE_EVENT_ENDED.
// NOTE: MF API signatures must be verified by the Windows CI build;
// this file is written from the documented frame-server pattern and
// has not yet been compiled on Windows in this Linux workspace.

#include <d3d11.h>
#include <mfapi.h>
#include <mfmediaengine.h>
#include <mfobjects.h>
#include <string>
#include <wrl/client.h>

namespace lws {

class VideoPlayer final : public IMFMediaEngineNotify {
public:
    VideoPlayer() = default;
    ~VideoPlayer();

    VideoPlayer(const VideoPlayer&) = delete;
    VideoPlayer& operator=(const VideoPlayer&) = delete;

    bool Initialize(ID3D11Device* device);
    bool Load(const std::wstring& path);
    void Play();
    void Pause();
    void Shutdown();

    // Call once per present cycle. Returns true if a new frame was
    // transferred to destinationTexture.
    bool Tick(ID3D11Texture2D* destinationTexture);
    bool HasError() const { return error_; }
    long long FramesTransferred() const { return frames_; }
    bool IsPlaying() const { return playing_; }

    // IMFMediaEngineNotify
    IFACEMETHODIMP EventNotify(DWORD event, DWORD_PTR param1, DWORD param2) override;
    IFACEMETHODIMP_(ULONG) AddRef() override;
    IFACEMETHODIMP_(ULONG) Release() override;
    IFACEMETHODIMP QueryInterface(REFIID riid, void** ppv) override;

private:
    void OnEnded();

    Microsoft::WRL::ComPtr<IMFMediaEngine> engine_;
    Microsoft::WRL::ComPtr<IMFDXGIDeviceManager> deviceManager_;
    UINT deviceManagerToken_ = 0;
    long long frames_ = 0;
    bool playing_ = false;
    bool error_ = false;
    bool loaded_ = false;
    bool mfStarted_ = false;
    ULONG refCount_ = 1;
};

}  // namespace lws
