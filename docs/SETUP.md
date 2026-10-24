# Complete setup — Windows development machine

## Prerequisites (all required)

1. Windows 10 version 1809+ or Windows 11, x64.
2. Visual Studio 2026 with: .NET desktop development, Desktop development with C++, Windows App SDK / WinUI workload, Windows SDK (26100 or later).
3. .NET 10 SDK (see `global.json` — SDK 10.0.100, rollForward latestFeature).
4. CMake 3.28+ and Git.
5. Node 20+ and npm, only for the backend (`src/backend/api`).
6. FFmpeg, only for the processing job (`src/backend/processing`).

## Step-by-step setup

```powershell
# 1. Extract the zip, open a Developer PowerShell for VS 2026
cd live-wallpaper-studio

# 2. Check the toolchain (fails fast with a clear message if something is missing)
.\scripts\bootstrap-windows.ps1

# 3. Create the local app folders (media, downloads, settings live here)
.\scripts\setup-windows.ps1

# 4. Build everything and run the tests
.\scripts\build-desktop.ps1
.\scripts\test-all.ps1

# 5. Launch the WinUI app from Visual Studio, or from the Release output:
#    src\desktop\LiveWallpaperStudio.App\bin\x64\Release\net10.0-windows10.0.26100.0\win-x64\LiveWallpaperStudio.App.exe
#    The renderer exe (lws-renderer.exe) is copied next to it by build-desktop.ps1.
```

## First run

1. Copy an H.264 MP4 to `%LOCALAPPDATA%\LiveWallpaperStudio\media`, or use a bundled file under `assets\wallpapers\video\`.
2. Open the app, go to Library, refresh, or go to Home and paste the video path.
3. Press Apply wallpaper. The native renderer plays it behind desktop icons.
4. If attachment is unsupported on your Windows build, the renderer stops with a clear error and leaves the desktop untouched — it never shows a topmost fake wallpaper.

## Backend setup (optional, staging only)

```powershell
cd src\backend\api
npm ci
npm run typecheck
npm test
# Copy .env.example to .env and fill DATABASE_URL / Hyperdrive, R2, Queues, OIDC values.
# Never commit .env. See docs/DEPLOYMENT.md.
```

## Troubleshooting

- **Renderer exe not found:** run `build-desktop.ps1` again; it copies `lws-renderer.exe` next to the app output.
- **WinUI build fails on TargetFramework:** check the Windows SDK version matches `net10.0-windows10.0.26100.0` and the App SDK package (2.5.1) restored.
- **Catalog empty in Discover:** `assets/catalog.json` must be next to the app exe, or the app honestly reports "not configured".
- **Video plays but icons are covered:** stop immediately, record OS build / GPU / display config, and file it as a shell-attachment failure — do not ship a workaround that covers icons.
- **MSIX packaging:** replace the Publisher identity in `packaging/msix/Package.appxmanifest` before packaging; unsigned QA builds are not production builds.
