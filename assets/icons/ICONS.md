# Icons — where each icon is used

## Realistic icons (new, in assets/icons/realistic/)

- `media-generation-realistic-app-icon-*.webp` — App icon (Home / window icon, MSIX logo source — convert to PNG for the manifest assets).
- `media-generation-realistic-library-icon-*.webp` — Library page / navigation.
- `media-generation-realistic-settings-icon-*.webp` — Settings page / navigation.
- `media-generation-realistic-download-icon-*.webp` — Downloads page / navigation.

## Simple icons (previous pack, in assets/icons/)

- `media-generation-app-icon-*.webp` — App logo (simple style).
- `media-generation-premium-crown-*.webp` — Premium badge, shown next to Premium / Premium Glass / 4K items in Discover.
- `media-generation-library-icon-*.webp` — Library (simple style).

## Navigation icons in the app

The NavigationView in MainWindow uses WinUI Symbol icons (Home, Folder, Globe, Download, View, Setting, Document, Help) so navigation works without image assets. The realistic images above are the branded assets for the app icon, Store listing, and page headers — convert them to PNG at the sizes the MSIX manifest requires (Square150x150, Square44x44, StoreLogo) before packaging. Do not ship WebP files as MSIX logos directly.
