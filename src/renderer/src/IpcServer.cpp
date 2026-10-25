#include "lws/IpcServer.h"

#include <iostream>
#include <vector>

namespace lws {

std::string ExtractJsonString(const std::string& json, const std::string& key) {
    std::string needle = "\"" + key + "\"";
    size_t pos = json.find(needle);
    if (pos == std::string::npos) return {};
    pos = json.find(':', pos + needle.size());
    if (pos == std::string::npos) return {};
    pos = json.find('"', pos);
    if (pos == std::string::npos) return {};
    size_t end = json.find('"', pos + 1);
    if (end == std::string::npos) return {};
    return json.substr(pos + 1, end - pos - 1);
}

void IpcServer::Run(Handler handler) {
    stopEvent_ = CreateEventW(nullptr, TRUE, FALSE, nullptr);
    while (stopEvent_ && WaitForSingleObject(stopEvent_, 0) != WAIT_OBJECT_0) {
        HANDLE pipe = CreateNamedPipeW(
            kPipeName, PIPE_ACCESS_DUPLEX,
            PIPE_TYPE_MESSAGE | PIPE_READMODE_MESSAGE | PIPE_WAIT,
            1, 1024 * 1024, 1024 * 1024, 0, nullptr);
        if (pipe == INVALID_HANDLE_VALUE) break;
        BOOL connected = ConnectNamedPipe(pipe, nullptr) ||
                         GetLastError() == ERROR_PIPE_CONNECTED;
        if (!connected) {
            CloseHandle(pipe);
            continue;
        }
        // A 1 MiB request is part of the protocol limit, but the default
        // Windows thread stack is also commonly 1 MiB. Keep this buffer on
        // the heap so starting IPC cannot overflow the renderer thread.
        std::vector<char> buffer(1024 * 1024);
        DWORD read = 0;
        if (ReadFile(pipe, buffer.data(), static_cast<DWORD>(buffer.size() - 1), &read, nullptr) && read > 0) {
            std::string request(buffer.data(), read);
            std::string method = ExtractJsonString(request, "method");
            std::string response = handler ? handler(method, request) : std::string{};
            DWORD written = 0;
            if (!WriteFile(pipe, response.data(), static_cast<DWORD>(response.size()), &written, nullptr)) {
                std::cerr << "IPC response write failed: " << GetLastError() << "\n";
            } else if (!FlushFileBuffers(pipe)) {
                std::cerr << "IPC response flush failed: " << GetLastError() << "\n";
            }
        } else {
            std::cerr << "IPC request read failed: " << GetLastError() << "\n";
        }
        DisconnectNamedPipe(pipe);
        CloseHandle(pipe);
    }
    if (stopEvent_) {
        CloseHandle(stopEvent_);
        stopEvent_ = nullptr;
    }
}

void IpcServer::Stop() {
    if (!stopEvent_) return;
    SetEvent(stopEvent_);
    // ConnectNamedPipe blocks until a client connects, so wake it with
    // a dummy connection that closes immediately. The loop then sees
    // the stop event and exits instead of hanging on join.
    HANDLE dummy = CreateFileW(kPipeName, GENERIC_WRITE, 0, nullptr,
                               OPEN_EXISTING, 0, nullptr);
    if (dummy != INVALID_HANDLE_VALUE) CloseHandle(dummy);
}

}  // namespace lws
