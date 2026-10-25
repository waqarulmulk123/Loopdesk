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
- `R2_INGEST` — private staging bucket
- `R2_PUBLISHED` — published assets bucket
- `INGEST_QUEUE` — processing messages
- `PUBLIC_CDN_BASE_URL` — immutable public CDN domain
- OIDC issuer/audience/client settings and an admin-role policy — required before exposing the admin routes or premium entitlements.
- `WORKER_ENVIRONMENT` — local/staging/production
- `SENTRY_DSN` (optional) — opt-in server observability

Cloudflare resources currently provisioned for both staging and production are one D1 database, two R2 buckets (ingest and published), one Queue, and the Workers API. The queue has no consumer by design: FFmpeg transcoding must run in a container or external job runner, never in a Worker request. Do not upload or publish content until that consumer and a licensed-media workflow are deployed.

The Worker currently has only a `workers.dev` address. A custom domain/CDN cannot be configured until a Cloudflare zone is added to the account.
