# Mixalune Control for Macro Deck 3 — Changelog

All notable changes to the Macro Deck plugin are recorded here. Versions follow semantic versioning; dates use YYYY-MM-DD.

## Unreleased

No unreleased changes yet.

## 0.1.2 — 2026-10-24

Store candidate; desktop/audio acceptance remains pending. The 0.1.1 GitHub tag targeted main and is superseded without a Store release.

### Added
- MIT-licensed Macro Deck plugin source, tests, and official Store build workflow.

### Changed
- Prepare 0.1.2 with Macro Deck SDK and CLI 3.0.0-beta.19 and AI asset disclosure.

### Fixed
- No unreleased fixes yet.

### Removed
- No unreleased removals yet.

## 0.1.0 — 2026-10-20

Development preview; actual desktop integration and live-audio acceptance testing remain pending.

### Added
- Set Channel Volume, Volume Up, Volume Down, Toggle Channel Mute, Set Channel Mute, Toggle Microphone Mute, and Reconnect to Mixalune.
- Dynamic channel discovery, custom channel names, stable saved IDs, and explicit unavailable selections.
- Game/stream and monitor volume controls, with shared mute.
- Authenticated local API 1.0 connection and token storage using Macro Deck's secret configuration API.
- Real-time state cache, button feedback, and connection, version, edition, volume, and mute variables.
- Automatic reconnection, heartbeat, command timeouts, capability checks, and state synchronization without stale command replay.
- Compiled plugin package, checksum, and setup instructions.

### Validation and limitations
- Built against Macro Deck SDK 3.0.0-beta.17 and .NET 10 for Windows x64.
- Automated WebSocket tests and stub-host conformance passed; these do not prove actual desktop or audio integration.
- Requires Mixalune local API 1.0. The inspected API does not report an edition.
- Volume percentages represent normalized fader travel, not native boosted gain percentages.
- No unfinished Plus/Pro actions or licensing overrides.

## Maintaining this log

1. Add each user-visible feature, change, or fix under Unreleased as it is made.
2. Before shipping, move those entries into a dated version section and start a fresh Unreleased section.
3. Record compatibility requirements, known limitations, and actual validation results.
4. Update the compiled installer, checksum, and setup instructions together.
5. Keep this plugin source synchronized with each build. Never commit user settings, credentials, generated outputs, Mixalune application source, or Stream Deck source.


