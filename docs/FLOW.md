# Complete user flow — premium desktop app

## First run (onboarding)

1. App launches, Mica dark theme, NavigationView with all pages.
2. If `HasCompletedOnboarding` is false, OnboardingPage shows first:
   Welcome → Pick a wallpaper → Preview → Apply → Stop anytime.
3. Press "Start — go to Home". The flag is saved locally, so returning users go straight to Home.

## Everyday flow (the happy path)

1. **Home:** status, display summary, and the animated banner are visible at a glance.
2. **Discover:** search titles/themes, browse Premium Themes, Premium Glass, 4K Animated. Premium items are labelled Premium.
3. **Preview:** on Home, paste or select a video path and press "Preview in app". It plays inside the app only (MediaPlayerElement), with transport controls.
4. **Apply:** press "Apply wallpaper". The supervisor starts the native renderer, which plays the video behind desktop icons, muted by default.
5. **Use Windows normally:** icons stay clickable, Start and windows work, fullscreen apps pause that monitor, lock/display-off/Battery Saver pause playback.
6. **Favourites:** save an ID in favourites.json (FavouritesService) and see it on the Favourites page. Fully offline, no account.
7. **Downloads:** downloaded files appear in the Downloads page with sizes.
8. **Settings:** change muted, pause rules, analytics/crash toggles, API URL, and press Save. Settings are local JSON only.
9. **Stop / exit:** press Stop on Home, or close the app. The supervisor stops the renderer and the tray icon is removed. No wallpaper is left covering icons.

## Error / edge flows (user-friendly, never a dead end)

- No video selected → "Choose a local H.264 MP4 file first." (no crash, no fake start).
- Renderer exe missing → "Renderer failed to start. See diagnostics." (Diagnostics page shows OS, renderer running or not, media folder, catalog found or not).
- Shell attachment unsupported → renderer exits with a clear error, desktop untouched, never a topmost fake wallpaper.
- Catalog not found → Discover honestly says "not configured", with no fake items.
- Corrupt settings / favourites / catalog → defaults / empty list, app still launches.
- Explorer restart / display change → renderer re-attaches or reports unsupported; it never covers icons.
- Low power → Battery Saver pause (and LowPowerMode setting) protects battery and GPU.

## Performance flow

- VSync presentation (`Present(1,0)`), hardware decode first with WARP fallback, pause when hidden/fullscreen, one renderer process per session (not per monitor).
- See docs/PERFORMANCE.md for the full performance model and test checklist.
