# Security

## Threat model

Malicious files, compromised content accounts, leaked presigned URLs,
unprivileged local processes, path traversal, hostile HTML wallpapers,
API abuse, and vulnerable multimedia parsers.

## Mandatory mitigations in this codebase

- HTTPS for all network traffic; OS TLS validation; no certificate bypass.
- R2 secrets and database credentials only in Cloudflare secret bindings or a secure server secret manager; never in the desktop binary.
- Named-pipe ACL restricts the current user/session; message size, type, protocol version and path allowlist are validated; network/UNC paths are rejected by default.
- No admin requirement for normal app behaviour; signed binaries and integrity-checked update packages.
- File decode in a separate process with bounded resources; all downloaded/imported inputs are treated as untrusted.
- WebView2 wallpapers (P1): isolated renderer, hardened permissions, no dangerous APIs or local filesystem bridges by default, explicit capability whitelist.
- OIDC with PKCE/system browser for account login; secure local token storage; MFA for administrators.
- Strict backend authorisation on every paid download and admin action; identity is not proof of entitlement.
- Rate limits, audit log, idempotent webhooks, privacy-aware telemetry.
- Telemetry off by default; no file names, window titles, process names or home directory contents sent to analytics by default.
- Crash reporting: local logs by default; upload only with opt-in consent, with tokens, paths, usernames, IPs and content titles redacted where practical.
- Third-party licences and codec patents: maintain NOTICE/SBOM. FFmpeg/mpv bundling requires a formal licence, build-option and patent evaluation.
