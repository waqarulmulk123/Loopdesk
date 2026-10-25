param(
    [Parameter(Mandatory = $true)]
    [string]$VideoPath,
    [string]$RendererPath = ''
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($RendererPath)) {
    $RendererPath = Join-Path $PSScriptRoot '..\build\renderer\Release\lws-renderer.exe'
}
$renderer = (Resolve-Path $RendererPath).Path
$video = (Resolve-Path $VideoPath).Path

Add-Type @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

public static class DesktopWindowProbe
{
    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr parent, EnumWindowsProc callback, IntPtr lParam);
    [DllImport("user32.dll")] private static extern int GetClassName(IntPtr hwnd, StringBuilder name, int maxCount);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
    [DllImport("user32.dll")] public static extern IntPtr GetParent(IntPtr hwnd);

    private static string ClassName(IntPtr hwnd)
    {
        var value = new StringBuilder(256);
        GetClassName(hwnd, value, value.Capacity);
        return value.ToString();
    }

    public static IntPtr FindWindowForProcess(int processId, string className)
    {
        IntPtr result = IntPtr.Zero;
        EnumWindows((hwnd, _) =>
        {
            if (Matches(hwnd, processId, className))
            {
                result = hwnd;
                return false;
            }
            EnumChildWindows(hwnd, (child, __) =>
            {
                if (Matches(child, processId, className))
                {
                    result = child;
                    return false;
                }
                return true;
            }, IntPtr.Zero);
            if (result != IntPtr.Zero) return false;
            return true;
        }, IntPtr.Zero);
        return result;
    }

    private static bool Matches(IntPtr hwnd, int processId, string className)
    {
        uint owner;
        GetWindowThreadProcessId(hwnd, out owner);
        return owner == (uint)processId && ClassName(hwnd) == className;
    }

    public static bool HasChildClass(IntPtr parent, string className)
    {
        bool found = false;
        EnumChildWindows(parent, (hwnd, _) =>
        {
            if (ClassName(hwnd) == className)
            {
                found = true;
                return false;
            }
            return true;
        }, IntPtr.Zero);
        return found;
    }

    public static string GetClass(IntPtr hwnd)
    {
        return ClassName(hwnd);
    }
}
'@

function Invoke-RendererRequest([string]$request) {
    $pipe = [System.IO.Pipes.NamedPipeClientStream]::new(
        '.', 'LiveWallpaperStudioRenderer',
        [System.IO.Pipes.PipeDirection]::InOut,
        [System.IO.Pipes.PipeOptions]::None)
    try {
        $pipe.Connect(5000)
        $pipe.ReadMode = [System.IO.Pipes.PipeTransmissionMode]::Message
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($request)
        $pipe.Write($bytes, 0, $bytes.Length)
        $pipe.Flush()
        $response = New-Object byte[] 1048576
        $count = $pipe.Read($response, 0, $response.Length)
        if ($count -lt 1) { throw 'Renderer pipe returned no response.' }
        return [System.Text.Encoding]::UTF8.GetString($response, 0, $count) | ConvertFrom-Json
    } finally {
        $pipe.Dispose()
    }
}

$process = Start-Process -FilePath $renderer -ArgumentList @('--video', $video) -WindowStyle Hidden -PassThru
try {
    Start-Sleep -Seconds 4
    if ($process.HasExited) { throw "Renderer exited early with code $($process.ExitCode)." }

    $wallpaper = [DesktopWindowProbe]::FindWindowForProcess($process.Id, 'LwsWallpaperWindow')
    if ($wallpaper -eq [IntPtr]::Zero) { throw 'Renderer did not expose its wallpaper HWND.' }
    $parent = [DesktopWindowProbe]::GetParent($wallpaper)
    if ($parent -eq [IntPtr]::Zero -or [DesktopWindowProbe]::GetClass($parent) -ne 'WorkerW') {
        throw 'Wallpaper HWND is not parented to WorkerW.'
    }
    if ([DesktopWindowProbe]::HasChildClass($parent, 'SHELLDLL_DefView')) {
        throw 'Wallpaper was attached to the icon-hosting WorkerW, which would risk obscuring icons.'
    }

    $first = Invoke-RendererRequest '{"protocolVersion":1,"requestId":"desktop-1","method":"engine.status","payload":{}}'
    Start-Sleep -Seconds 3
    $second = Invoke-RendererRequest '{"protocolVersion":1,"requestId":"desktop-2","method":"engine.status","payload":{}}'
    $shutdown = Invoke-RendererRequest '{"protocolVersion":1,"requestId":"desktop-3","method":"engine.shutdown","payload":{}}'
    if (-not $process.WaitForExit(10000)) { throw 'Renderer did not stop after engine.shutdown.' }
    if (-not $first.success -or -not $second.success -or -not $shutdown.success) { throw 'Renderer rejected an acceptance-test request.' }
    if ($second.payload.frames -le $first.payload.frames) { throw 'Video frames did not advance.' }

    [pscustomobject]@{
        ParentClass = [DesktopWindowProbe]::GetClass($parent)
        IconHost = $false
        FirstFrame = $first.payload.frames
        SecondFrame = $second.payload.frames
        ExitCode = $process.ExitCode
    }
} finally {
    if (-not $process.HasExited) { Stop-Process -Id $process.Id -Force }
}
