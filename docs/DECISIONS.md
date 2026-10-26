# Product decisions

| ID | Decision | Status |
| --- | --- | --- |
| D1 | Local import is intentionally unavailable; Loopdesk never scans drives or accepts arbitrary user paths. | Current behavior |
| D2 | Protected downloads open in the Loopdesk preview. No downloads-folder command is exposed. | Current behavior |
| D3 | The catalog endpoint is managed by default. Offline-only mode is a future settings flow. | Current behavior |
| D4 | Premium catalog assets remain unavailable until an entitlement provider is selected. | Current behavior |
| D5 | Catalog cards are sourced from the live catalog; no mock-up-only titles should ship as catalog data. | Current behavior |
| D6 | The Home greeting is derived from local time. | Current behavior |
| D7 | The app has no account system; the header avatar is application identity only. | Current behavior |
| D8 | Analytics and crash reporting default off. No data is sent until a real opt-in implementation exists. | Current behavior |
| D9 | Cache management must work only with Loopdesk-managed, protected downloads. | Pending implementation |
| D10 | Home routes users to Discover; download and apply stay in the catalog/library flow. | Current behavior |
