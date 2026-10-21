# Mixalune Control for Stream Deck — Changelog

All notable changes to the Elgato Stream Deck plugin are recorded here.
Versions follow semantic versioning. Dates use YYYY-MM-DD.

## Unreleased

### Added
- No unreleased additions yet.

### Changed
- No unreleased changes yet.

### Fixed
- No unreleased fixes yet.

### Removed
- No unreleased removals yet.

## 0.1.0 — 2026-10-21

Development preview; live Stream Deck and audio acceptance testing is pending.

### Added
- Seven key actions: Set Channel Volume, Volume Up, Volume Down, Toggle Channel Mute, Set Channel Mute, Toggle Microphone Mute, and Reconnect to Mixalune.
- Dynamic channel discovery with custom names, stable IDs, and preserved unavailable selections.
- Separate game and monitor volume selection, using normalized fader percentages.
- Confirmed live button titles and mute states, with offline and unavailable feedback.
- Local API 1.0 authentication and Windows-account-encrypted token storage.
- Connection settings, connection test, persistent connect/disconnect, and status information.
- Automatic reconnection, timeout handling, heartbeat, wake recovery, and event resubscription without replaying stale commands.
- Self-contained Windows x64 installer validated by Elgato's official CLI.
- Setup and troubleshooting guidance, 29 automated tests, browser UI checks, and packaged executable smoke testing.

### Limitations
- Requires Windows 10/11 x64, Stream Deck 6.9+, and a Mixalune build exposing local API 1.0.
- Key actions only; dials and Multi Actions are not included.
- The inspected Mixalune API does not report an edition; shown as Not reported.
- Mock/simulated-host tests do not certify physical-device compatibility or live audio stability.

## Maintaining this log

1. Describe each user-visible feature, behavior change, or fix under Unreleased as it is made.
2. Before shipping, move those entries into a dated version section and create a fresh Unreleased section.
3. Record compatibility requirements, known limitations, and actual validation results.
4. Update the installer, its SHA-256 checksum, and setup instructions together.
5. Publish compiled distribution files only; never add private implementation source, debug symbols, user settings, or credentials to the public branch.
