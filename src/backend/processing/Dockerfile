FROM alpine:3.21
RUN apk add --no-cache ffmpeg
WORKDIR /work
COPY transcode.sh /work/transcode.sh
RUN chmod +x /work/transcode.sh
# Usage: docker run --rm -v "$PWD:/work" lws-transcoder ./transcode.sh input.mp4 output.mp4
# Transcoding never runs inside a time-constrained Worker HTTP request.
ENTRYPOINT ["./transcode.sh"]
