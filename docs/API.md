# Cloud API — /v1

Base: Workers + Hono. Stateless HTTP API, validated input/output.
See `src/backend/api/src/index.ts` for the skeleton implementation.

| Method | Endpoint | Purpose | Access |
|---|---|---|---|
| GET | /v1/health | Availability, API version, build | Public |
| GET | /v1/config | Feature flags, min client version, supported media | Public |
| GET | /v1/wallpapers | Cursor-paginated catalog, filters, sort | Public |
| GET | /v1/wallpapers/:id | Detail and variants | Public |
| GET | /v1/categories | Categories and counts | Public |
| GET | /v1/search?q=... | Search with cursor/limit | Public |
| POST | /v1/assets/:variantId/authorize-download | Entitlement check + download method | Guest/public or authenticated per asset |
| GET | /v1/me/favorites | Account favourites | Authenticated (P1) |
| PUT | /v1/me/favorites/:id | Add favourite | Authenticated (P1) |
| DELETE | /v1/me/favorites/:id | Remove favourite | Authenticated (P1) |
| POST | /v1/admin/uploads/init | Validated, limited upload intent | Admin |
| POST | /v1/admin/uploads/complete | Confirm upload, enqueue processing | Admin |
| GET | /v1/admin/jobs/:id | Process status and errors | Admin |
| POST | /v1/admin/wallpapers/:id/publish | Atomically publish valid variants | Admin |
| POST | /v1/admin/wallpapers/:id/unpublish | Remove from discovery and future authorisation | Admin |
| GET | /v1/admin/audit | Admin actions pagination | Admin |

## Rules

- Cursor pagination, deterministic sort with tie-breaker, limit max 100.
- Anonymous catalog GETs may be cached at the edge for a short bounded period; no private account data in shared cache.
- Signed/authorised media access is generated server-side only.
- Premium: private R2 + short-lived authorised delivery. R2 S3 presigned URLs work through the S3 API endpoint, NOT through a custom CDN domain. Public assets use the custom CDN domain after publication.
- Client honours 429 and Retry-After.
- Admin access requires a credible identity provider with MFA and RBAC. Do not invent an insecure password database.
