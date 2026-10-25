param(
  [Parameter(Mandatory = $true)][string]$InputPath,
  [Parameter(Mandatory = $true)][string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$input = (Resolve-Path -LiteralPath $InputPath).Path
$output = [System.IO.Path]::GetFullPath($OutputPath)
$outputDirectory = Split-Path -Parent $output
if (-not (Test-Path -LiteralPath $outputDirectory)) { New-Item -ItemType Directory -Path $outputDirectory | Out-Null }

ffmpeg -y -i $input -map 0:v:0 -c:v libx264 -pix_fmt yuv420p -movflags +faststart -an $output
if ($LASTEXITCODE -ne 0) { throw 'FFmpeg failed.' }

$probe = ffprobe -v error -select_streams v:0 -show_entries stream=width,height,r_frame_rate -of json $output | ConvertFrom-Json
$stream = $probe.streams[0]
if (-not $stream.width -or -not $stream.height) { throw 'No video stream was produced.' }
$rate = $stream.r_frame_rate -split '/'
$fps = [math]::Round(([double]$rate[0] / [double]$rate[1]))
$item = Get-Item -LiteralPath $output

[pscustomobject]@{
  file = $output
  contentType = 'video/mp4'
  width = [int]$stream.width
  height = [int]$stream.height
  fps = [int]$fps
  sizeBytes = [int64]$item.Length
  sha256 = (Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash.ToLowerInvariant()
} | ConvertTo-Json
