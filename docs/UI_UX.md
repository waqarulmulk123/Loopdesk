# UI / UX — premium design system

## Principles

- **Calm premium, not loud:** dark Mica theme, one accent (purple #7C5CFF) for primary actions only, gold (#FFC94D) only for Premium badges.
- **One primary action per screen:** Home = Apply, Library = Refresh/Search, Discover = Search/Refresh, Settings = Save.
- **No dead ends:** every error state tells the user what happened and what to do next (see docs/FLOW.md).
- **No fake content:** empty states say "not configured" or "no favourites yet", never invented items.

## Tokens (App.xaml)

- Colours: PremiumAccent #7C5CFF, PremiumAccentLight #9D8CFF, PremiumGold #FFC94D, PremiumCard #1E1E24, TextPrimary white, TextSecondary #B8B8C0.
- Corner radius: cards 12, buttons 8.
- Page padding: 24. Title 28, subtitle 18, body 14.
- Font: system Segoe UI Variable (WinUI default), no custom font download.

## Motion

- EntranceThemeTransition on every page.
- Home banner pulses (Storyboard, AutoReverse, forever while loaded).
- ListViews use WinUI item transitions. Animations are subtle and stop when the page is unloaded; the wallpaper itself is the main motion, not the chrome.

## Accessibility / user-friendly

- All buttons have text labels, not icon-only actions.
- ToggleSwitches have headers; TextBoxes have placeholders and headers where they collect input.
- High-contrast dark theme, large touch targets (default WinUI button sizes), keyboard navigation via NavigationView (standard WinUI focus order).
- Screen-reader: page titles are TitleTextBlockStyle, status text is plain text (no colour-only meaning — Premium is also written as text, not just gold).

## Performance (UX side)

- Pages load data on construction and refresh on demand (no polling).
- Catalog and library lists are plain ListViews over local data — no network on Home/Library load.
- Preview is opt-in (press Preview), so the app does not decode video on launch.
