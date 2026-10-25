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
    $first = Invoke-RendererRequest '{"protocolVersion":1,"requestId":"smoke-1","method":"engine.status","payload":{}}'
    Start-Sleep -Seconds 3
    $second = Invoke-RendererRequest '{"protocolVersion":1,"requestId":"smoke-2","method":"engine.status","payload":{}}'
    $shutdown = Invoke-RendererRequest '{"protocolVersion":1,"requestId":"smoke-3","method":"engine.shutdown","payload":{}}'
    if (-not $process.WaitForExit(10000)) { throw 'Renderer did not stop after engine.shutdown.' }
    if (-not $first.success -or -not $second.success -or -not $shutdown.success) { throw 'Renderer rejected a smoke-test request.' }
    if ($second.payload.frames -le $first.payload.frames) { throw 'Video frames did not advance.' }
    [pscustomobject]@{ FirstFrame = $first.payload.frames; SecondFrame = $second.payload.frames; ExitCode = $process.ExitCode }
} finally {
    if (-not $process.HasExited) { Stop-Process -Id $process.Id -Force }
}
