#include "lws/VideoMediaFoundation.h"

#include <mfapi.h>

namespace lws {

VideoPlayer::~VideoPlayer() { Shutdown(); }

bool VideoPlayer::Initialize(ID3D11Device* device) {
    if (!device || FAILED(MFStartup(MF_VERSION))) return false;
    mfStarted_ = true;
    if (FAILED(MFCreateDXGIDeviceManager(&deviceManagerToken_, deviceManager_.GetAddressOf()))) return false;
    if (FAILED(deviceManager_->ResetDevice(device, deviceManagerToken_))) return false;

    Microsoft::WRL::ComPtr<IMFAttributes> attributes;
    if (FAILED(MFCreateAttributes(attributes.GetAddressOf(), 4))) return false;
    if (FAILED(attributes->SetUnknown(MF_MEDIA_ENGINE_CALLBACK, static_cast<IUnknown*>(this)))) return false;
    if (FAILED(attributes->SetUINT32(MF_MEDIA_ENGINE_VIDEO_OUTPUT_FORMAT, DXGI_FORMAT_B8G8R8A8_UNORM))) return false;
    if (FAILED(attributes->SetUnknown(MF_MEDIA_ENGINE_DXGI_MANAGER, static_cast<IUnknown*>(deviceManager_.Get())))) return false;

    Microsoft::WRL::ComPtr<IMFMediaEngineClassFactory> factory;
    if (FAILED(CoCreateInstance(CLSID_MFMediaEngineClassFactory, nullptr, CLSCTX_INPROC_SERVER,
                                 IID_PPV_ARGS(factory.GetAddressOf())))) return false;
    if (FAILED(factory->CreateInstance(0, attributes.Get(), engine_.GetAddressOf()))) return false;
    return true;
}

bool VideoPlayer::Load(const std::wstring& path) {
    if (!engine_) return false;
    HRESULT hr = engine_->SetSource(const_cast<wchar_t*>(path.c_str()));
    if (FAILED(hr)) {
        error_ = true;
        return false;
    }
    loaded_ = true;
    return true;
}

void VideoPlayer::Play() {
    if (engine_ && loaded_) {
        engine_->Play();
        playing_ = true;
    }
}

void VideoPlayer::Pause() {
    if (engine_) {
        engine_->Pause();
        playing_ = false;
    }
}

void VideoPlayer::Shutdown() {
    if (engine_) {
        engine_->Shutdown();
        engine_.Reset();
    }
    deviceManager_.Reset();
    if (mfStarted_) {
        MFShutdown();
        mfStarted_ = false;
    }
    playing_ = false;
    loaded_ = false;
}

bool VideoPlayer::Tick(ID3D11Texture2D* destinationTexture) {
    if (!engine_ || !playing_ || !destinationTexture) return false;
    LONGLONG pts = 0;
    // In frame-server mode the app drives the tick; a new frame is
    // available when OnVideoStreamTick returns S_OK with a valid pts.
    HRESULT hr = engine_->OnVideoStreamTick(&pts);
    if (FAILED(hr)) return false;
    hr = engine_->TransferVideoFrame(destinationTexture, nullptr, nullptr, nullptr, 0);
    if (SUCCEEDED(hr)) {
        ++frames_;
        return true;
    }
    return false;
}

void VideoPlayer::OnEnded() {
    // Loop: seek to start and keep playing. No reinitialisation, so the
    // loop join stays as tight as the asset allows.
    if (engine_) {
        engine_->SetCurrentTime(0.0);
        engine_->Play();
    }
}

IFACEMETHODIMP VideoPlayer::EventNotify(DWORD event, DWORD /*param1*/, DWORD /*param2*/) {
    switch (static_cast<MF_MEDIA_ENGINE_EVENT>(event)) {
        case MF_MEDIA_ENGINE_EVENT_ENDED:
            OnEnded();
            break;
        case MF_MEDIA_ENGINE_EVENT_ERROR:
            error_ = true;
            playing_ = false;
            break;
        default:
            break;
    }
    return S_OK;
}

IFACEMETHODIMP_(ULONG) VideoPlayer::AddRef() { return ++refCount_; }
IFACEMETHODIMP_(ULONG) VideoPlayer::Release() {
    ULONG count = --refCount_;
    if (count == 0) delete this;
    return count;
}
IFACEMETHODIMP VideoPlayer::QueryInterface(REFIID riid, void** ppv) {
    if (!ppv) return E_POINTER;
    if (riid == __uuidof(IMFMediaEngineNotify) || riid == IID_IUnknown) {
        *ppv = static_cast<IMFMediaEngineNotify*>(this);
        AddRef();
        return S_OK;
    }
    *ppv = nullptr;
    return E_NOINTERFACE;
}

}  // namespace lws
