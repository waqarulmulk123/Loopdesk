import assert from 'node:assert/strict';
import test from 'node:test';
import app from '../src/index.js';

const env = {};

test('health endpoint returns the public API contract', async () => {
  const response = await app.fetch(new Request('https://api.test/v1/health'), env);
  assert.equal(response.status, 200);
  assert.deepEqual(await response.json(), { status: 'ok', apiVersion: 'v1', build: '0.2.0' });
});

test('catalog is empty without a database and rejects invalid limits', async () => {
  const catalog = await app.fetch(new Request('https://api.test/v1/wallpapers'), env);
  assert.equal(catalog.status, 200);
  assert.deepEqual(await catalog.json(), { items: [], nextCursor: null, limit: 24, catalogVersion: null });

  const invalid = await app.fetch(new Request('https://api.test/v1/wallpapers?limit=0'), env);
  assert.equal(invalid.status, 400);
});
