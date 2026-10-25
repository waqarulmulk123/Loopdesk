import { createServer } from 'node:http';
import { mkdtemp, readFile, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { execFile } from 'node:child_process';
import { promisify } from 'node:util';

const run = promisify(execFile);

createServer(async (request, response) => {
  if (request.url === '/health') return response.writeHead(200).end('ok');
  if (request.method !== 'POST' || request.url !== '/transcode') return response.writeHead(404).end();
  const work = await mkdtemp(join(tmpdir(), 'lws-'));
  try {
    const input = join(work, 'input.mp4');
    const output = join(work, 'output.mp4');
    await writeFile(input, Buffer.concat(await readBody(request)));
    await run('ffmpeg', ['-y', '-i', input, '-map', '0:v:0', '-c:v', 'libx264', '-pix_fmt', 'yuv420p', '-movflags', '+faststart', '-an', output]);
    const { stdout } = await run('ffprobe', ['-v', 'error', '-select_streams', 'v:0', '-show_entries', 'stream=width,height,r_frame_rate', '-of', 'json', output]);
    const stream = JSON.parse(stdout).streams?.[0];
    if (!stream?.width || !stream?.height) throw new Error('No video stream produced.');
    const [numerator, denominator = '1'] = String(stream.r_frame_rate).split('/');
    response.writeHead(200, { 'content-type': 'video/mp4', 'x-width': stream.width, 'x-height': stream.height, 'x-fps': Number(numerator) / Number(denominator) });
    response.end(await readFile(output));
  } catch (error) {
    response.writeHead(422, { 'content-type': 'text/plain' }).end(error instanceof Error ? error.message : 'Transcode failed.');
  } finally {
    await rm(work, { recursive: true, force: true });
  }
}).listen(8080, '0.0.0.0');

function readBody(request) {
  return new Promise((resolve, reject) => {
    const chunks = [];
    request.on('data', chunk => chunks.push(chunk));
    request.on('end', () => resolve(chunks));
    request.on('error', reject);
  });
}
