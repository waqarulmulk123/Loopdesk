# Release gates and evidence

This is the release-control record for Loopdesk. Passing CI means that the
source tree builds; it does not prove that a Windows desktop wallpaper behaves
correctly on a customer's machine.

## Non-negotiable gates

| Gate | Evidence required | Owner | Current disposition |
| --- | --- | --- | --- |
| Content rights and confidentiality | Rights record for every distributed asset; no premium source video in public Git history | Product owner | Repository remediation completed 2026-10-27: every reachable GitHub branch and the rewritten `main` tree contain zero MP4 files. The private object store remains the only media distribution path. Rights records are still an owner responsibility. |
| Legal notices | A selected Loopdesk product licence and an up-to-date [NOTICE](../NOTICE.md) | Copyright owner | NOTICE index added; product licence is an owner decision. |
| Reproducible package | Successful Windows workflow for the release commit, SHA-256 for each delivered file, and a retained workflow URL | Release manager | CI creates unsigned QA ZIP/MSIX artifacts only. |
| Signing and publisher identity | Signing certificate or Store package identity, timestamped signature verification, and revocation plan | Publisher-account owner | Cannot be created from source control. |
| Store disclosure | Partner Center listing matches the tested package. If the package has an external runtime dependency, disclose it in the first two description lines. | Partner Center owner | The 2026-10-27 certification report requested .NET Desktop Runtime disclosure. Re-evaluate against the exact self-contained MSIX before resubmission. |
| Desktop acceptance | Completed physical-machine evidence for the matrix below | Windows test owner | CI cannot satisfy this gate. |

## Public-media remediation

Completed on 2026-10-27 with the product owner's authorization:

1. A verified private Git bundle backup was created before rewriting history.
2. `git filter-repo` removed every `*.mp4` path from repository history.
3. The rewritten `main` was force-pushed with an explicit lease; stale
   Dependabot branches that retained the old media snapshots were deleted.
4. Verification confirmed one remote branch (`main`), zero MP4 entries in its
   complete GitHub tree, zero tracked MP4s, and zero reachable MP4 objects
   locally. The repository ignores future `*.mp4` additions.

Existing clones, forks, downloaded archives, and GitHub's internal retention
window are outside source-control's control. Ask any collaborator or fork
owner to delete old copies; keep all source media in private storage.

## Required physical Windows evidence

Record Windows edition/build, GPU/driver, monitor topology, package SHA-256,
and PASS/FAIL/NOT RUN for each result:

- Windows 10 22H2 and Windows 11 22H2/23H2/24H2: download, apply, icon
  click/drag, and focus behavior.
- Close-to-tray, explicit Exit, sign-out/restart, sleep/resume, and Explorer
  restart. Confirm the configured wallpaper survives only where the product
  promises it does.
- Add/remove a second monitor, DPI changes (100/125/150/200%), and GPU/display
  recovery. Do not claim multi-monitor support until this passes.
- Offline launch and use of already verified downloads; failed/cancelled
  downloads; disk-full and insufficient-permission paths.
- Clean-machine MSIX install/uninstall/reinstall and a standard-user launch.
- Keyboard navigation, Narrator labels, high contrast, and scaling checks.

Attach screenshots or short recordings to the release record. A green CI run,
a build log, or a screenshot of the app window alone is not acceptance
evidence for desktop-shell behavior.

## Release procedure

1. Start from a reviewed commit on `main`; ensure all CI workflows are green.
2. Complete [RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md) and this document's
   legal, content, Store, and physical-test gates.
3. Run the Windows build workflow for the exact commit. Treat its ZIP and MSIX
   as QA artifacts until the signing and physical gates pass.
4. Compute and publish checksums, for example:

   ```powershell
   Get-FileHash .\Loopdesk_*.msix -Algorithm SHA256
   Get-AuthenticodeSignature .\Loopdesk_*.msix
   ```

5. Upload only the tested Store package through Partner Center, or distribute a
   signed direct installer with matching checksum and notices.
6. Record the commit SHA, workflow URL, artifact SHA-256, signature subject and
   timestamp, test evidence, and rollback contact in the release notes.

## Actions that source control cannot perform

- choosing the legal owner and product licence;
- authorizing a destructive Git history rewrite and coordinating forks;
- acquiring/using a code-signing identity and Partner Center publisher account;
- changing Cloudflare account resources or production secrets; and
- executing the physical Windows and hardware acceptance matrix.
