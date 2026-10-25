#pragma once

// Minimal named-pipe IPC server. Protocol: JSON envelope, protocolVersion 1,
// max 1 MiB, methods from the shared protocol schema. Never accept executable
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

bool TryExtractJsonString(const std::string& json, const std::string& key,
                          std::string* value);
std::string EscapeJsonString(const std::string& value);

}  // namespace lws
