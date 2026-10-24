#pragma once

// Minimal named-pipe IPC server (M1, to be hardened in M5).
// Protocol: JSON envelope, protocolVersion 1, max 1 MiB, methods from
// the shared protocol schema. This M1 parser extracts "method" and
// "requestId" by string search; M3 replaces it with a proper JSON
// parser and ACL-restricted pipe security. Never accept executable
// code, network paths, or unvalidated paths from IPC.

#include <string>
#include <windows.h>

namespace lws {

class IpcServer {
public:
    static constexpr const wchar_t* kPipeName = L"\\\\.\\pipe\\LiveWallpaperStudioRenderer";

    // Blocking single-connection loop. Returns when the client
    // disconnects or when Stop() is called from another thread.
    // The handler receives the method name and the raw request, and
    // returns the full JSON response.
    using Handler = std::string (*)(const std::string& method, const std::string& request);

    void Run(Handler handler);
    void Stop();

private:
    HANDLE stopEvent_ = nullptr;
};

std::string ExtractJsonString(const std::string& json, const std::string& key);

}  // namespace lws
