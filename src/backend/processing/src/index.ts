// Cloudflare Queues consumer + FFmpeg processing job (spec section 11).
// This Worker never transcodes inside an HTTP request: it consumes
// ingest messages, downloads the staged file from the private R2
// ingest bucket, transcodes it with FFmpeg (see processing/transcode.sh
// for the standalone job, and the container job for production), and
// uploads the validated output to the published bucket. On failure
// it marks the ingest job as failed with the error, and does NOT
// publish the wallpaper.
//
// Deployment note: a Workers Queue consumer that shells out to FFmpeg
// needs a container / external job runner in production. This file
// documents the exact message contract and state transitions so the
// container job and the Worker share one protocol. Do not deploy
// this as a plain Worker and expect ffmpeg to be available.

export type IngestMessage = {
  jobId: string;
  wallpaperId: string;
  sourceKey: string;
  targetKey: string;
};

export function validateMessage(body: unknown): IngestMessage | null {
  if (!body || typeof body !== 'object') return null;
  const m = body as Record<string, unknown>;
  if (typeof m.jobId !== 'string' || typeof m.wallpaperId !== 'string') return null;
  if (typeof m.sourceKey !== 'string' || typeof m.targetKey !== 'string') return null;
  return m as IngestMessage;
}

export default {
  async queue(batch: { messages: { body: unknown }[] }): Promise<void> {
    for (const message of batch.messages) {
      const parsed = validateMessage(message.body);
      if (!parsed) {
        // Malformed messages are dropped, not retried forever.
        continue;
      }
      // Real implementation (container job): download parsed.sourceKey
      // from R2_INGEST, run transcode.sh, validate with ffprobe,
      // upload to R2_PUBLISHED as parsed.targetKey, then update
      // ingest_jobs.state = 'completed' in D1. On any
      // error, set status = 'failed' with the error text.
      throw new Error(
        `Processing job not configured in this build: job ${parsed.jobId} requires the container FFmpeg job.`,
      );
    }
  },
};
