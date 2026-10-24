# Generate a deterministic local sample video for offline testing.
# Requires ffmpeg on PATH. This is a developer tool, not a runtime
# dependency — the app must start with bundled fixtures even if
# ffmpeg is not installed on the user's machine.
$ErrorActionPreference = "Stop"

if ($null -eq (Get-Command ffmpeg -ErrorAction SilentlyContinue)) {
    Write-Host "ffmpeg is not on PATH. Install it, or copy a compatible H.264 MP4 into samples/ instead."
    exit 1
}

New-Item -ItemType Directory -Force -Path "samples" | Out-Null
ffmpeg -hide_banner -y -f lavfi -i "testsrc2=size=1280x720:rate=30" `
  -t 8 -an -c:v libx264 -preset veryfast -pix_fmt yuv420p `
  -movflags +faststart "samples\sample-loop.mp4"
Write-Host "Wrote samples\sample-loop.mp4"
