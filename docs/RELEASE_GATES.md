# Release gates and evidence

This is the release-control record for Loopdesk. Passing CI means that the
source tree builds; it does not prove that a Windows desktop wallpaper behaves
correctly on a customer's machine.

## Non-negotiable gates

| Gate | Evidence required | Owner | Current disposition |
| --- | --- | --- | --- |
| Content rights and confidentiality | Rights record for every distributed asset; no premium source video in public Git history | Product owner | Blocked: the current tree tracks 21 MP4 files, and 16 are labelled `premium: true` by the legacy seed data. |
| Legal notices | A selected Loopdesk product licence and an up-to-date [NOTICE](../NOTICE.md) | Copyright owner | NOTICE index added; product licence is an owner decision. |
| Reproducible package | Successful Windows workflow for the release commit, SHA-256 for each delivered file, and a retained workflow URL | Release manager | CI creates unsigned QA ZIP/MSIX artifacts only. |
| Signing and publisher identity | Signing certificate or Store package identity, timestamped signature verification, and revocation plan | Publisher-account owner | Cannot be created from source control. |
| Store disclosure | Partner Center listing matches the tested package. If the package has an external runtime dependency, disclose it in the first two description lines. | Partner Center owner | The 2026-10-27 certification report requested .NET Desktop Runtime disclosure. Re-evaluate against the exact self-contained MSIX before resubmission. |
| Desktop acceptance | Completed physical-machine evidence for the matrix below | Windows test owner | CI cannot satisfy this gate. |

## Public-media remediation

The repository currently makes the tracked MP4s available to every Git clone.
The legacy SQLite seed labels 16 of the 21 tracked MP4s as premium. Changing a
row, removing a download route, or deleting files in a future commit does not
remove them from existing Git history.

This needs explicit owner authorization because it rewrites published history:

1. Inventory each media file, its copyright owner, licence, and intended
   visibility; move distributable catalog objects to the appropriate R2
   bucket, keeping premium originals in private storage.
2. Make an immutable backup and notify all collaborators and downstream forks.
3. Use an approved history-rewrite procedure (for example `git filter-repo`)
   to remove the private source assets from every reachable ref; force-push
   only after the owner approves the impact.
4. Rotate any URLs, credentials, or other secrets that may have been present in
   the same history; ask fork owners to delete old clones and forks where
   possible.
5. Run the tag release gate again. It intentionally rejects a tagged release
   while MP4s are still tracked.

No automated workflow can make files already cloned by another party private.

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
