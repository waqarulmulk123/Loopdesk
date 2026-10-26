# Product decisions

| ID | Decision | Status |
| --- | --- | --- |
| D1 | Import accepts a single user-selected MP4 only; Loopdesk never scans drives. | Pending implementation |
| D2 | Protected downloads open in Loopdesk preview. The downloads-folder command opens only Loopdesk-managed app data. | Pending implementation |
| D3 | The catalog endpoint is managed by default. Offline-only mode is a future settings flow. | Current behavior |
| D4 | Premium catalog assets remain unavailable until an entitlement provider is selected. | Current behavior |
| D5 | Catalog cards are sourced from the live catalog; no mock-up-only titles should ship as catalog data. | In progress |
| D6 | The Home greeting will be derived from local time. | Pending implementation |
| D7 | The app has no account system; the header avatar is application identity only. | Current behavior |
| D8 | Analytics and crash reporting default off. No data is sent until a real opt-in implementation exists. | Current behavior |
| D9 | Cache management must work only with Loopdesk-managed, protected downloads. | Pending implementation |
| D10 | Home apply must download the featured catalog item when necessary before invoking the native renderer. | Pending implementation |
