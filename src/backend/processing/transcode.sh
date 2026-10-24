#!/bin/sh
# Transcode an input video to a broadly compatible H.264 MP4 baseline.
# Usage: transcode.sh <input> <output>
# Validates with ffprobe first; fails loudly on malformed input.
set -eu
INPUT="$1"
OUTPUT="$2"
ffprobe -v error -select_streams v:0 -show_entries stream=codec_name,width,height -of default=noprint_wrappers=1 "$INPUT"
ffmpeg -hide_banner -y -i "$INPUT" -an -c:v libx264 -preset medium -pix_fmt yuv420p -movflags +faststart "$OUTPUT"
echo "Transcoded: $OUTPUT"
