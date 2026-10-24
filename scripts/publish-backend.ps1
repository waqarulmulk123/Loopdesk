# Publish the backend to staging (M4, manual step).
# Requires an authenticated Cloudflare account and configured secrets.
# Do not run this automatically in CI without explicit approval.
$ErrorActionPreference = "Stop"

Set-Location "src\backend\api"
npm ci
npm run lint
npm run typecheck
npm test
npm run deploy:staging
Set-Location "..\..\.."
Write-Host "Backend published to staging. Record the deployed endpoint (without secrets) in docs/IMPLEMENTATION_STATUS.md."
