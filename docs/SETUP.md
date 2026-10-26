# Complete setup — Windows development machine

## Prerequisites (all required)

1. Windows 10 version 1809+ or Windows 11, x64.
2. Visual Studio 2026 with: .NET desktop development, Desktop development with C++, Windows App SDK / WinUI workload, Windows SDK (26100 or later).
3. .NET 10 SDK (see `global.json` — SDK 10.0.100, rollForward latestFeature).
4. CMake 3.28+ and Git.
5. Node 20+ and npm, only for the backend (`src/backend/api`).
6. FFmpeg, optional but recommended to prepare media before publishing with `scripts\prepare-wallpaper.ps1`.

## Step-by-step setup

```powershell
# 1. Clone the repository, then open a Developer PowerShell for VS 2026
git clone https://github.com/waqarulmulk123/Loopdesk.git
cd Loopdesk

# 2. Check the toolchain (fails fast with a clear message if something is missing)
.\scripts\bootstrap-windows.ps1

# 3. Create the local app folders (protected downloads and settings live here)
.\scripts\setup-windows.ps1

# 4. Build everything and run the tests
.\scripts\build-desktop.ps1
.\scripts\test-all.ps1

# 5. Launch the WinUI app from Visual Studio, or from the Release output:
#    src\desktop\LiveWallpaperStudio.App\bin\x64\Release\net10.0-windows10.0.26100.0\win-x64\LiveWallpaperStudio.App.exe
#    The renderer exe (lws-renderer.exe) is copied next to it by build-desktop.ps1.
```

## First run

1. Open Discover and select a published free wallpaper from the cloud catalog.
2. Download it. Loopdesk verifies its SHA-256 and size, then stores it in its per-user protected vault.
3. Open My wallpapers, select the downloaded item, and press Apply selected. The native renderer plays it behind desktop icons.
4. If attachment is unsupported on your Windows build, the renderer stops with a clear error and leaves the desktop untouched — it never shows a topmost fake wallpaper.

Loopdesk does not import, scan, or display arbitrary local drive files. Local media preparation belongs to the authenticated admin publishing workflow, not the consumer desktop app.

## Backend setup (optional)

```powershell
cd src\backend\api
npm ci
npm run typecheck
npm test
# Local mode has no D1 binding and returns an empty catalog.
# Deploys use the named D1 and published-R2 bindings in wrangler.jsonc.
# Never commit secrets. See docs/DEPLOYMENT.md.
```

## Troubleshooting

- **Renderer exe not found:** run `build-desktop.ps1` again; it copies `lws-renderer.exe` next to the app output.
- **WinUI build fails on TargetFramework:** check the Windows SDK version matches `net10.0-windows10.0.26100.0` and the App SDK package (2.5.1) restored.
- **Catalog empty in Discover:** verify connectivity to the deployed HTTPS Workers API, then use Refresh catalog. The desktop catalog is cloud-managed; no local `catalog.json` is used.
- **Video plays but icons are covered:** stop immediately, record OS build / GPU / display config, and file it as a shell-attachment failure — do not ship a workaround that covers icons.
- **MSIX packaging:** the Store identity is already configured in `src/desktop/LiveWallpaperStudio.App/Package.appxmanifest`. Build the Store MSIX from that project; Microsoft re-signs MSIX packages after Store certification.
