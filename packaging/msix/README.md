# Loopdesk Microsoft Store packaging

The supplied Loopdesk artwork is included as `Assets/StoreLogo.png`,
`Assets/Square150x150Logo.png`, and `Assets/Square44x44Logo.png`. The
manifest is associated with Microsoft Store product `9NNBCZ64H0LX` and uses
the Store-assigned package identity `MWaqarUlMulk.Loopdesk`.

## Store (MSIX)

- The desktop project can build a Store MSIX from its in-project manifest.
  Upload the generated MSIX from the release workflow to Partner Center.
- Microsoft Store re-signs MSIX packages after certification, so no paid code
  signing certificate is needed for Store-only distribution. The final package
  must still pass the M5 release checklist.

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
