# Manual Windows acceptance checklist

Run these tests on a physical Windows installation before release. Automated build and Core tests do not prove desktop-shell behavior.

- [ ] Windows 10 22H2: apply a downloaded wallpaper behind Explorer icons; icons remain clickable.
- [ ] Windows 11 22H2, 23H2, 24H2, and later: repeat the Explorer attachment test.
- [ ] Close the UI, stop the wallpaper, and exit the app; verify expected renderer lifetime and cleanup.
- [ ] Download a catalog wallpaper; confirm progress, checksum validation, privacy-preserving vault storage, preview, and apply behavior.
- [ ] Disconnect the network; confirm the catalog error state and existing downloaded wallpapers remain usable.
- [ ] Lock/unlock, full-screen app, display-off, battery-saver, and remote-session pause behavior after those policies are implemented.
- [ ] Attach and remove a second monitor; verify per-display assignment and hot-plug behavior after multi-monitor support is implemented.
- [ ] Restart Explorer and confirm icon click/drag behavior manually; the renderer structural reattach proof passed on Windows 10 build 19045, while display-resolution/DPI and GPU-reset recovery remain untested.
- [ ] Test keyboard-only navigation, Narrator labels, and 100%, 125%, 150%, and 200% display scaling.
- [ ] Install and uninstall the MSIX on a clean Windows machine.
