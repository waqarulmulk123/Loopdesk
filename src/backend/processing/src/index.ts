import { Container, getContainer } from '@cloudflare/containers';

type IngestMessage = { jobId: string; wallpaperId: string; variantId: string; sourceKey: string; targetKey: string };

interface Env {
  DB: D1Database;
  R2_INGEST: R2Bucket;
  R2_PUBLISHED: R2Bucket;
  TRANSCODER: DurableObjectNamespace<MediaTranscoder>;
}

export class MediaTranscoder extends Container {
  defaultPort = 8080;
  sleepAfter = '5m';
  enableInternet = false;
  pingEndpoint = 'localhost/health';
}

function parseMessage(body: unknown): IngestMessage | null {
  if (!body || typeof body !== 'object') return null;
  const value = body as Record<string, unknown>;
  return typeof value.jobId === 'string' && typeof value.wallpaperId === 'string' && typeof value.variantId === 'string' && typeof value.sourceKey === 'string' && typeof value.targetKey === 'string'
    ? value as IngestMessage
    : null;
}

async function fail(env: Env, jobId: string, error: string): Promise<void> {
  await env.DB.prepare("UPDATE ingest_jobs SET state = 'failed', error_code = ?, updated_at = CURRENT_TIMESTAMP WHERE id = ?").bind(error.slice(0, 500), jobId).run();
}

export default {
  async queue(batch: MessageBatch<unknown>, env: Env): Promise<void> {
    for (const message of batch.messages) {
      const job = parseMessage(message.body);
      if (!job) {
        message.ack();
        continue;
      }
      try {
        const source = await env.R2_INGEST.get(job.sourceKey);
        if (!source?.body) throw new Error('Staged source object is missing.');
        await env.DB.prepare("UPDATE ingest_jobs SET state = 'processing', updated_at = CURRENT_TIMESTAMP WHERE id = ?").bind(job.jobId).run();
        const response = await getContainer(env.TRANSCODER, job.jobId).fetch(new Request('http://container/transcode', { method: 'POST', headers: { 'content-type': 'video/mp4' }, body: source.body }));
        if (!response.ok || !response.body) throw new Error(`Transcoder rejected source (${response.status}).`);
        const output = await response.arrayBuffer();
        const width = Number(response.headers.get('x-width'));
        const height = Number(response.headers.get('x-height'));
        const fps = Number(response.headers.get('x-fps'));
        if (!Number.isInteger(width) || !Number.isInteger(height) || !Number.isFinite(fps) || output.byteLength === 0) throw new Error('Transcoder returned invalid video metadata.');
        const digest = await crypto.subtle.digest('SHA-256', output);
        const sha256 = [...new Uint8Array(digest)].map(value => value.toString(16).padStart(2, '0')).join('');
        await env.R2_PUBLISHED.put(job.targetKey, output, { httpMetadata: { contentType: 'video/mp4' } });
        await env.DB.batch([
          env.DB.prepare("UPDATE media_variants SET width = ?, height = ?, fps = ?, size_bytes = ?, sha256 = ?, processing_state = 'ready' WHERE id = ?").bind(width, height, fps, output.byteLength, sha256, job.variantId),
          env.DB.prepare("UPDATE ingest_jobs SET state = 'completed', error_code = NULL, updated_at = CURRENT_TIMESTAMP WHERE id = ?").bind(job.jobId),
        ]);
        message.ack();
      } catch (error) {
        const description = error instanceof Error ? error.message : 'Unknown processing error.';
        await fail(env, job.jobId, description);
        message.retry();
      }
    }
  },
} satisfies ExportedHandler<Env>;
