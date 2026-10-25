# Deployment

## Desktop

```powershell
.\scripts\bootstrap-windows.ps1
.\scripts\build-desktop.ps1 -Configuration Debug -Architecture x64
.\scripts\test-all.ps1
```

See `packaging/msix/README.md` for Store MSIX and signed direct
installer notes. Signing is required for production; unsigned
artifacts are QA only.

## Backend (staging and production, with an authenticated Cloudflare account)

```powershell
cd src\backend\api
npm ci
npm run lint
npm run typecheck
npm test
npm run dev
npm run deploy:staging
# Run this only after staging acceptance:
# npm run deploy:production
```

Required bindings / environment (templates only, never secrets in git):

- `DB` — D1 catalog binding. It is configured in `wrangler.jsonc` for staging and production.
- `R2_PUBLISHED` — published assets bucket
- `PUBLIC_CDN_BASE_URL` — immutable public CDN domain
- OIDC issuer/audience/client settings and an admin-role policy — required before exposing the admin routes or premium entitlements.
- `WORKER_ENVIRONMENT` — local/staging/production
- `SENTRY_DSN` (optional) — opt-in server observability

The free deployment requires only the Workers API, one D1 database, and the published R2 bucket in each environment. Existing ingest buckets and Queues may remain in the account, but the API does not use them.

The Worker currently has only a `workers.dev` address. A custom domain/CDN cannot be configured until a Cloudflare zone is added to the account.

Until then, non-premium media is served by the API at `/v1/assets/:variantId/content` through the Worker’s `workers.dev` address. This preserves R2 privacy and gives the desktop a real HTTPS download URL without requiring a domain.

## Free media preparation

Prepare every asset locally before uploading. `src/backend/processing/transcode.sh` remains an optional local FFmpeg helper, but it is not deployed. This avoids Cloudflare Containers and Queue-consumer costs entirely.

On Windows, create an upload-ready file and metadata with:

```powershell
.\scripts\prepare-wallpaper.ps1 -InputPath .\source.mp4 -OutputPath .\published.mp4
```

Use the returned JSON fields when calling the admin upload endpoint.
