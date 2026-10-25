# Packaging — M5, not yet complete

The supplied Loopdesk artwork is included as `Assets/StoreLogo.png`,
`Assets/Square150x150Logo.png`, and `Assets/Square44x44Logo.png`. The package
identity must still be replaced with the publisher's real signing identity
before an MSIX can be built.

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
