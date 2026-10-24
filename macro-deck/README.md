# Mixalune Control for Macro Deck 3

Version **0.1.0** — development preview.

[Download the plugin](https://github.com/knowledgezip/Mixalune/raw/refs/heads/feature/mixalune-macro-deck/macro-deck/downloads/com.knowledgezip.mixalune-control-0.1.0.macroDeckPlugin) · [SHA-256 checksum](downloads/com.knowledgezip.mixalune-control-0.1.0.macroDeckPlugin.sha256) · [Setup instructions](SETUP.txt) · [Changelog](CHANGELOG.md)

## Setup

Requires Windows 10/11 x64, Macro Deck 3 beta.17 or later, and a Mixalune build exposing local API 1.0 with API Settings. The inspected SDK version is beta.17; compatibility with future host versions must be verified.

1. Download the `.macroDeckPlugin` file.
2. In Macro Deck, choose Integrations > Install from file and select the package.
3. In Mixalune, enable the local API and copy its authentication token.
4. Configure Mixalune Control with the matching port (default 18765) and token.
5. Add an action to a button and select a discovered channel.

## Controls

Set channel volume, increase/decrease volume, toggle/set channel mute, toggle microphone mute, and reconnect. Includes dynamic channel discovery, custom names, stable saved IDs, event-driven button/variable feedback, secure token storage, and automatic reconnection without replaying old commands.

Game/stream and monitor volumes can be controlled separately; mute is shared. Percentages represent normalized fader travel, including boost, and can differ from native gain percentages.

## Development status

32 automated tests passed and package validation was clean. Stub-host conformance and mock-server testing do not certify actual Macro Deck/Mixalune desktop integration or audio stability. Live application, sleep/resume, and audio-dropout testing remain pending. This unsigned development preview is not a Macro Deck Store release.

## Updates and distribution

Record new features and fixes under **Unreleased** in [CHANGELOG.md](CHANGELOG.md). For each release, add a dated version entry and update the compiled package, checksum, and setup instructions together.

The Macro Deck plugin is now open source under MIT; see [source and build instructions](source/README.md). The historical 0.1.0 download above remains a development preview. Version 0.1.2 is being prepared for Store review using SDK beta.19. Mixalune application and Stream Deck source are not included.

The [existing 0.1.0 prerelease](https://github.com/knowledgezip/Mixalune/releases/tag/mixalune-control-v0.1.0) remains available. The [Elgato Stream Deck plugin](https://github.com/knowledgezip/Mixalune/tree/feature/mixalune-stream-deck) has its own separate branch and package format.


