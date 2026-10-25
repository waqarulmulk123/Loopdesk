# Cloud API — /v1

Base: Workers + Hono. Stateless HTTP API, validated input/output.
See `src/backend/api/src/index.ts` for the deployed D1-backed implementation.

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
| POST | /v1/admin/uploads/init | Create a direct-publish MP4 upload intent | Admin |
| PUT | /v1/admin/uploads/:jobId/content | Store a pre-encoded H.264 MP4 and complete its job | Admin |
| GET | /v1/admin/jobs/:id | Process status and errors | Admin |
| POST | /v1/admin/wallpapers/:id/publish | Atomically publish valid variants | Admin |
| POST | /v1/admin/wallpapers/:id/unpublish | Remove from discovery and future authorisation | Admin |
| GET | /v1/admin/audit | Admin actions pagination | Admin |

## Rules

- Cursor pagination, deterministic sort with tie-breaker, limit max 100.
- Anonymous catalog GETs may be cached at the edge for a short bounded period; no private account data in shared cache.
- Public media is served only by the API after a catalog record has been published. Premium remains unavailable until a real entitlement provider exists.
- Client honours 429 and Retry-After.
- Admin access requires a credible identity provider with MFA and RBAC. Do not invent an insecure password database.

## Free direct-publish workflow

The free deployment does not transcode media in Cloudflare. Prepare a non-premium H.264 MP4 locally with FFmpeg, obtain the width, height, FPS, byte size and SHA-256, then send that metadata to `POST /v1/admin/uploads/init`. Upload the exact MP4 to the returned `uploadUrl` with `Content-Type: video/mp4` and matching `Content-Length`. The job completes immediately; publish it with `POST /v1/admin/wallpapers/:id/publish`.
