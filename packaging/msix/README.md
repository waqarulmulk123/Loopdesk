# Packaging — M5, not yet complete

## Store (MSIX)

- Package identity, Store-managed updates, and framework dependencies
  are set in the Visual Studio packaging project. That project is not
  yet in this repository; the WinUI app project above is the
  unpackaged build target for M3.
- Do not ship an MSIX until the M5 release checklist passes.

## Direct installer

- Signed direct installer via WiX or Inno Setup, with a bootstrapper
  that installs the Windows App SDK runtime if needed.
- Production installer and EXE/MSIX must be signed with
  organization-provided credentials or an eligible signing service.
  Signing improves trust but does not guarantee no SmartScreen prompt.
- Unsigned test artifacts are for QA only.

## Uninstall

- Remove binaries and auto-start hooks.
- Ask whether user data/media should also be deleted.
- Never delete user-imported originals.
