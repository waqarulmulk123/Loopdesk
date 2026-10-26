#include "lws/IpcServer.h"

#include <cctype>
#include <iomanip>
#include <iostream>
#include <sddl.h>
#include <sstream>
#include <vector>

namespace lws {

bool TryExtractJsonString(const std::string& json, const std::string& key,
                          std::string* value) {
    if (!value) return false;
    std::string needle = "\"" + key + "\"";
    size_t pos = json.find(needle);
    if (pos == std::string::npos) return false;
    pos = json.find(':', pos + needle.size());
    if (pos == std::string::npos) return false;
    ++pos;
    while (pos < json.size() && std::isspace(static_cast<unsigned char>(json[pos]))) ++pos;
    if (pos >= json.size() || json[pos++] != '"') return false;

    std::string parsed;
    while (pos < json.size()) {
        char current = json[pos++];
        if (current == '"') {
            *value = std::move(parsed);
            return true;
        }
        if (static_cast<unsigned char>(current) < 0x20) return false;
        if (current != '\\') {
            parsed += current;
            continue;
        }
        if (pos >= json.size()) return false;
        char escaped = json[pos++];
        switch (escaped) {
        case '"': parsed += '"'; break;
        case '\\': parsed += '\\'; break;
        case '/': parsed += '/'; break;
        case 'b': parsed += '\b'; break;
        case 'f': parsed += '\f'; break;
        case 'n': parsed += '\n'; break;
        case 'r': parsed += '\r'; break;
        case 't': parsed += '\t'; break;
        default: return false;
        }
    }
    return false;
}

std::string EscapeJsonString(const std::string& value) {
    std::ostringstream escaped;
    for (unsigned char current : value) {
        switch (current) {
        case '"': escaped << "\\\""; break;
        case '\\': escaped << "\\\\"; break;
        case '\b': escaped << "\\b"; break;
        case '\f': escaped << "\\f"; break;
        case '\n': escaped << "\\n"; break;
        case '\r': escaped << "\\r"; break;
        case '\t': escaped << "\\t"; break;
        default:
            if (current < 0x20) {
                escaped << "\\u" << std::hex << std::setw(4) << std::setfill('0')
                        << static_cast<int>(current) << std::dec << std::setfill(' ');
            } else {
                escaped << static_cast<char>(current);
            }
        }
    }
    return escaped.str();
}

void IpcServer::Run(Handler handler) {
    stopEvent_ = CreateEventW(nullptr, TRUE, FALSE, nullptr);
    PSECURITY_DESCRIPTOR securityDescriptor = nullptr;
    // Owner Rights resolves to the renderer process owner (the interactive
    // user), so another local account cannot open the control pipe.
    if (!ConvertStringSecurityDescriptorToSecurityDescriptorW(
            L"D:P(A;;GA;;;OW)", SDDL_REVISION_1, &securityDescriptor, nullptr)) {
        std::cerr << "IPC security descriptor creation failed: " << GetLastError() << "\n";
        if (stopEvent_) CloseHandle(stopEvent_);
        stopEvent_ = nullptr;
        return;
    }
    SECURITY_ATTRIBUTES securityAttributes{};
    securityAttributes.nLength = sizeof(securityAttributes);
    securityAttributes.lpSecurityDescriptor = securityDescriptor;
    while (stopEvent_ && WaitForSingleObject(stopEvent_, 0) != WAIT_OBJECT_0) {
        HANDLE pipe = CreateNamedPipeW(
            kPipeName, PIPE_ACCESS_DUPLEX,
            PIPE_TYPE_MESSAGE | PIPE_READMODE_MESSAGE | PIPE_WAIT,
            1, 1024 * 1024, 1024 * 1024, 0, &securityAttributes);
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
            std::string method;
            TryExtractJsonString(request, "method", &method);
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
    LocalFree(securityDescriptor);
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
