# Third-party notices

This file is a distribution notice index. It is **not** a licence grant for
Loopdesk. The copyright owner must choose and add a product licence before
publishing source code or a direct installer.

The release owner must re-run licence review against the exact lockfiles and
published package before every public release. Do not treat this short index as
a substitute for the full licence texts shipped by the dependencies.

| Component | Locked version | Licence / notice source |
| --- | --- | --- |
| Microsoft Windows App SDK | 2.5.1 | `Microsoft.WindowsAppSDK` NuGet package `license.txt` and `NOTICE.txt`; its licence permits redistribution of files binplaced with the application subject to its distribution requirements. |
| CommunityToolkit.Mvvm | 8.4.0 | MIT — NuGet package metadata and <https://github.com/CommunityToolkit/dotnet>. |
| System.Security.Cryptography.ProtectedData | 10.0.0 | MIT — NuGet package metadata and <https://github.com/dotnet/dotnet>. |
| Hono | resolved from `src/backend/api/package-lock.json` | MIT — <https://github.com/honojs/hono>. |
| Cloudflare Workers runtime | hosted service | Governed by the Cloudflare service agreement; it is not redistributed with the desktop package. |

## Release-owner obligations

1. Preserve the complete notices required by every dependency included in the
   exact MSIX or direct package. In particular, retain the Windows App SDK
   `NOTICE.txt` when its redistributed files require it.
2. Verify rights and attribution for every wallpaper, poster, icon, font and
   screenshot. An asset being in the repository is not evidence of a right to
   distribute it.
3. Add a product `LICENSE` only after the copyright owner chooses its terms.
   Do not copy a third-party licence into `LICENSE`, because that would
   incorrectly licence Loopdesk itself.
4. Keep the actual release package, its SHA-256, dependency-lockfile revision,
   and this notice revision together in the release record.
