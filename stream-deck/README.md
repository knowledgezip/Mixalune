# Mixalune Control for Elgato Stream Deck

Version **0.1.0** — development preview, 2026-10-21.

[Download the plugin](https://github.com/knowledgezip/Mixalune/raw/refs/heads/feature/mixalune-stream-deck/stream-deck/downloads/com.knowledgezip.mixalune.streamDeckPlugin) · [SHA-256 checksum](downloads/com.knowledgezip.mixalune.streamDeckPlugin.sha256) · [Setup instructions](SETUP.txt) · [Changelog](CHANGELOG.md)

## Requirements and setup

Windows 10/11 x64, Elgato Stream Deck 6.9 or later, and Mixalune with local API 1.0 enabled. The runtime is included. This is an Elgato plugin, separate from Macro Deck 3.

1. Download and open the `.streamDeckPlugin` file with Stream Deck.
2. In Mixalune, enable API Settings and copy its authentication token.
3. Drag a Mixalune Control action onto a Stream Deck key.
4. Select the key, enter the token under Connection, and choose Save & connect.
5. Choose the channel and any volume or mute settings for that button.

## Included controls

- Set Channel Volume, Volume Up, and Volume Down, with game/monitor bus selection.
- Toggle Channel Mute and Set Channel Mute.
- Toggle Microphone Mute.
- Reconnect to Mixalune.

Channels are discovered dynamically. Buttons show confirmed volume/mute state, and the connection recovers automatically after an unexpected disconnect. Credentials are encrypted for the Windows account and excluded from exported button settings. Volume percentages represent normalized fader positions, not Mixalune's boosted gain percentages.

## Validation and limitations

29 automated tests, browser settings-screen checks, packaged-executable smoke testing, and official Elgato CLI validation passed. Testing used a mock Mixalune server and simulated Stream Deck host. Physical Stream Deck operation, live audio stability, and actual installed-host compatibility still need acceptance testing.

This preview supports key actions, not dials or Multi Actions. Edition is shown as Not reported when Mixalune does not supply it. No Plus/Pro functionality or licensing override is included.

## Updates

Record features and fixes in [CHANGELOG.md](CHANGELOG.md) under **Unreleased**. At each release, move those entries into a dated version section, then update the installer, checksum, and setup instructions together.

This is a binary distribution branch. Do not upload the private development folder. The package excludes C# source and debug symbols; required HTML/JavaScript settings-screen assets remain inspectable, and binaries can still be reverse engineered.
