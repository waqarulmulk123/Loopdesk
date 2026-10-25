// Native renderer executable.
// Usage:
//   lws-renderer.exe                 startup proof only (CI-safe, exits 0)
//   lws-renderer.exe --video <path>  attach a wallpaper window behind
//                                    desktop icons and play the video in
//                                    a loop. If attachment fails, the
//                                    window is destroyed and the process
//                                    exits with ShellAttachmentUnsupported
//                                    behaviour (code 2) — it never shows
//                                    a topmost fake wallpaper.
// M1 is in progress: this code has not yet passed the M1 gate on a
// real Windows desktop, and must not be described as working until it does.

#include <atomic>
#include <iostream>
#include <string>
#include <thread>

#include "lws/DesktopShellAdapter.h"
#include "lws/DesktopWindow.h"
#include "lws/DisplayManager.h"
#include "lws/Dx11Device.h"
#include "lws/IpcServer.h"
#include "lws/PlaybackPolicy.h"
#include "lws/SwapChainPresenter.h"
#include "lws/VideoMediaFoundation.h"

// Shared with the IPC handler thread. The handler only flips these
// flags; the render loop owns the player and the window.
namespace {
std::atomic<lws::VideoPlayer*> g_player{nullptr};
std::atomic<bool> g_paused{false};
std::atomic<bool> g_shutdownRequested{false};

std::string IpcHandler(const std::string& method, const std::string& request) {
    std::string requestId = lws::ExtractJsonString(request, "requestId");
    std::string state = "Playing";
    if (g_paused.load()) state = "Paused";
    if (method == "engine.hello") {
        return "{\"protocolVersion\":1,\"requestId\":\"" + requestId +
               "\",\"kind\":\"response\",\"success\":true,\"payload\":{\"state\":\"Ready\",\"capabilities\":[\"video\"]},\"error\":null}";
    }
    if (method == "engine.status") {
        auto* player = g_player.load();
        long long frames = player ? player->FramesTransferred() : 0;
        return "{\"protocolVersion\":1,\"requestId\":\"" + requestId +
               "\",\"kind\":\"response\",\"success\":true,\"payload\":{\"state\":\"" + state +
               "\",\"frames\":" + std::to_string(frames) + "},\"error\":null}";
    }
    if (method == "playback.pause") {
        g_paused.store(true);
        if (auto* player = g_player.load()) player->Pause();
        return "{\"protocolVersion\":1,\"requestId\":\"" + requestId + "\",\"kind\":\"response\",\"success\":true,\"payload\":{\"state\":\"Paused\"},\"error\":null}";
    }
    if (method == "playback.resume") {
        g_paused.store(false);
        if (auto* player = g_player.load()) player->Play();
        return "{\"protocolVersion\":1,\"requestId\":\"" + requestId + "\",\"kind\":\"response\",\"success\":true,\"payload\":{\"state\":\"Playing\"},\"error\":null}";
    }
    if (method == "engine.shutdown") {
        g_shutdownRequested.store(true);
        return "{\"protocolVersion\":1,\"requestId\":\"" + requestId + "\",\"kind\":\"response\",\"success\":true,\"payload\":{\"state\":\"Stopping\"},\"error\":null}";
    }
    return "{\"protocolVersion\":1,\"requestId\":\"" + requestId + "\",\"kind\":\"response\",\"success\":false,\"payload\":null,\"error\":{\"code\":\"InvalidRequest\",\"message\":\"Unknown or unsupported method\"}}";
}
}  // namespace

int RunVideo(const std::wstring& videoPath) {
    const HRESULT comResult = CoInitializeEx(nullptr, COINIT_MULTITHREADED);
    const bool comInitialized = SUCCEEDED(comResult);
    if (FAILED(comResult) && comResult != RPC_E_CHANGED_MODE) {
        std::cout << "Failed to initialize COM for Media Foundation.\n";
        return 1;
    }

    lws::Dx11Device device;
    if (!device.Create()) {
        std::wcout << L"D3D11 failed: " << device.Diagnostics() << L"\n";
        if (comInitialized) CoUninitialize();
        return 1;
    }

    int width = GetSystemMetrics(SM_CXSCREEN);
    int height = GetSystemMetrics(SM_CYSCREEN);
    HMONITOR primary = MonitorFromPoint({0, 0}, MONITOR_DEFAULTTOPRIMARY);

    lws::DesktopWindow window;
    if (!window.Create(primary, width, height)) {
        std::cout << "Failed to create wallpaper window.\n";
        if (comInitialized) CoUninitialize();
        return 1;
    }

    lws::DesktopShellAdapter adapter;
    auto attach = adapter.TryAttach(window.Handle(), primary);
    if (!attach.success) {
        std::wcout << L"ShellAttachmentUnsupported: " << attach.diagnostics << L"\n";
        window.Destroy();
        if (comInitialized) CoUninitialize();
        return 2;
    }
    std::wcout << L"Attached via " << attach.strategyName << L"\n";

    lws::SwapChainPresenter presenter;
    if (!presenter.Create(device.Device(), window.Handle(), width, height)) {
        std::cout << "Failed to create swap chain presenter.\n";
        adapter.Detach(window.Handle());
        window.Destroy();
        if (comInitialized) CoUninitialize();
        return 1;
    }

    // VideoPlayer is heap-allocated because it is COM ref-counted.
    auto* player = new lws::VideoPlayer();
    g_player.store(player);
    if (!player->Initialize(device.Device()) || !player->Load(videoPath)) {
        std::cout << "Failed to load video with Media Foundation.\n";
        g_player.store(nullptr);
        player->Release();
        presenter.Destroy();
        adapter.Detach(window.Handle());
        window.Destroy();
        if (comInitialized) CoUninitialize();
        return 1;
    }
    player->Play();

    // IPC server on its own thread. The supervisor (WinUI app in M3)
    // talks to it over \\.\pipe\LiveWallpaperStudioRenderer.
    lws::IpcServer ipc;
    std::thread ipcThread([&ipc]() { ipc.Run(IpcHandler); });

    lws::PlaybackPolicy policy(lws::PausePolicy{});
    MSG msg{};
    bool running = true;
    while (running && !g_shutdownRequested.load()) {
        while (PeekMessageW(&msg, nullptr, 0, 0, PM_REMOVE)) {
            if (msg.message == WM_QUIT) running = false;
            TranslateMessage(&msg);
            DispatchMessageW(&msg);
        }
        if (!running) break;
        if (device.IsLost() || player->HasError()) break;
        if (!g_paused.load() && !policy.ShouldPause(primary, false, false, false, false)) {
            if (player->Tick(presenter.BackBuffer())) {
                presenter.Present();
            }
        }
        Sleep(16);
    }

    ipc.Stop();
    if (ipcThread.joinable()) ipcThread.join();
    g_player.store(nullptr);
    player->Shutdown();
    player->Release();
    presenter.Destroy();
    adapter.Detach(window.Handle());
    window.Destroy();
    if (comInitialized) CoUninitialize();
    return 0;
}

int main(int argc, char** argv) {
    std::cout << "LiveWallpaperStudio renderer (M1 in progress)\n";
    std::cout << "Displays found: " << lws::ListDisplays() << "\n";

    if (argc >= 3 && std::string(argv[1]) == "--video") {
        // Convert the UTF-8 argument to wide text for Windows APIs.
        const int needed = MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS,
                                                argv[2], -1, nullptr, 0);
        if (needed <= 1) {
            std::cout << "Video path must be valid, non-empty UTF-8.\n";
            return 1;
        }
        std::wstring path(static_cast<size_t>(needed), L'\0');
        if (!MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, argv[2], -1,
                                 path.data(), needed)) {
            std::cout << "Video path is not valid UTF-8.\n";
            return 1;
        }
        path.pop_back();  // Keep the conversion buffer's terminating NUL out of the path.
        return RunVideo(path);
    }

    lws::Dx11Device device;
    if (device.Create()) {
        std::wcout << L"D3D11: " << device.Diagnostics() << L"\n";
    } else {
        std::wcout << L"D3D11 failed: " << device.Diagnostics() << L"\n";
        return 1;
    }
    std::cout << "Renderer startup OK. Use --video <path> on a real Windows desktop for M1 playback.\n";
    return 0;
}
