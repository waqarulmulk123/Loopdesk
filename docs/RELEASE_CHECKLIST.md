# Release checklist — spec section 21

Do NOT describe this product as fully working until every item below
is actually true on real Windows hardware. As of 2026-10-24, code is
written locally; Windows CI verification is pending after the final push.

- [ ] Native WinUI app compiles and launches on actual target Windows
- [ ] Native renderer compiles for x64 and actually plays moving video behind icons
- [ ] Wallpaper window does not steal focus, block icons, or obscure the desktop in tested configurations
- [ ] Local imports work, valid media plays, invalid media fails safely
- [ ] Loop, fit modes, pause/resume, tray, startup preference, multi-monitor and crash handling tested
- [ ] Offline launch and previously downloaded media playback verified
- [ ] SQLite persistent settings/cache survive reboots and crash scenarios
- [ ] Download manager tested with real Range/ETag/cancellation/checksum behaviour
- [ ] Workers API deployed to staging, PostgreSQL schema migrated, real R2 object retrieved
- [ ] Catalog filters and authorised-download flows work when API is configured
- [ ] At least one admin upload → transcode → validate → publish workflow tested end-to-end
- [ ] Required unit, integration and shell acceptance tests pass, results archived with dates/build numbers
- [ ] App and renderer do not require elevated privileges during normal playback
- [ ] Installer and clean uninstall validated on at least one fresh test machine
- [ ] Security/third-party licence documentation, README and operational guides included
- [ ] No hardcoded production secrets, fabricated claims, fake settings actions or non-working placeholder buttons
- [ ] Windows versions/GPU codecs tested are listed accurately; unsupported legacy OSs are not advertised
- [ ] P1 features not implemented are documented as not-yet-implemented, not shown as enabled menu items

## Final agent report (to be filled after Windows verification)

1. Source repository tree and main entrypoints
2. Versions/SDKs used and reasons for deviations from the spec
3. Commands to build, run, test and package on Windows
4. Screenshots or verified recordings of real desktop wallpaper playback, or an explicit statement that no GUI test environment was available
5. Test matrix with PASS / FAIL / NOT RUN, number of automated tests and environment
6. Cloudflare setup variables and deployed staging endpoints (without secrets), if configured
7. Features fully implemented; P1 features not implemented; blockers and reproducible issues
8. Installer artifact names, checksum and how signing is performed
9. Risk notes about undocumented desktop shell behaviour and tested Windows builds
