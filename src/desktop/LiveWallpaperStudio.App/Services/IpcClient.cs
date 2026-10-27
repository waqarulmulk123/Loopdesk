using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace LiveWallpaperStudio.App.Services;

/// <summary>
/// Named-pipe client for the renderer (protocol v1). Sends one JSON
/// message and reads one JSON response. Used by the supervisor in M3
/// for status and shutdown; the renderer in M1/M2 implements the
/// matching server side.
/// </summary>
public static class IpcClient
{
    private const int MaxMessageBytes = 1024 * 1024;

    public static async Task<string?> SendAsync(string method, string requestId, CancellationToken ct = default)
    {
        await using var client = new NamedPipeClientStream(
            ".", "LiveWallpaperStudioRenderer", PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(TimeSpan.FromSeconds(5), ct);
        client.ReadMode = PipeTransmissionMode.Message;
        string request = JsonSerializer.Serialize(new
        {
            protocolVersion = 1,
            requestId,
            kind = "request",
            method,
            payload = new { },
        });
        byte[] bytes = Encoding.UTF8.GetBytes(request);
        await client.WriteAsync(bytes, ct);
        await client.FlushAsync(ct);

        byte[] buffer = new byte[8192];
        using var response = new MemoryStream();
        do
        {
            int read = await client.ReadAsync(buffer, ct);
            if (read == 0)
            {
                return null;
            }
            if (response.Length + read > MaxMessageBytes)
            {
                throw new InvalidDataException("Renderer response exceeds the IPC message limit.");
            }
            response.Write(buffer, 0, read);
        }
        while (!client.IsMessageComplete);

        return Encoding.UTF8.GetString(response.GetBuffer(), 0, (int)response.Length);
    }
}
