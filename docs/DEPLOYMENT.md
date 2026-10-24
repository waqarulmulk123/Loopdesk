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

## Backend (staging only, with an authenticated Cloudflare account)

```powershell
cd src\backend\api
npm ci
npm run lint
npm run typecheck
npm test
npm run dev
# Deployment only with authenticated Cloudflare account and secrets:
# npm run deploy:staging
```

Required bindings / environment (templates only, never secrets in git):

- `DATABASE_URL` / Hyperdrive binding — PostgreSQL connection
- `R2_INGEST` — private staging bucket
- `R2_PUBLISHED` — published assets bucket
- `INGEST_QUEUE` — processing messages
- `PUBLIC_CDN_BASE_URL` — immutable public CDN domain
- `APP_PUBLIC_ORIGIN` — admin CORS origin
- `OIDC_ISSUER`, `OIDC_AUDIENCE`, `OIDC_CLIENT_ID` — login/admin auth
- `ADMIN_ROLE` — authorisation policy
- `WORKER_ENVIRONMENT` — local/staging/production
- `SENTRY_DSN` (optional) — opt-in server observability

When cloud is not configured, the local app remains fully functional
and the online tab says "Online catalog not configured" instead of
showing fake content.
