# Mixalune Control for Macro Deck 3

Version **0.2.0** — noise-gate beta candidate.

[Download the plugin](https://github.com/knowledgezip/Mixalune/raw/refs/heads/feature/mixalune-macro-deck/macro-deck/downloads/com.knowledgezip.mixalune-control-0.2.0.macroDeckPlugin) · [SHA-256 checksum](downloads/com.knowledgezip.mixalune-control-0.2.0.macroDeckPlugin.sha256) · [Setup instructions](SETUP.txt) · [Changelog](CHANGELOG.md)

## Setup

Requires Windows 10/11 x64, Macro Deck 3 beta.19 or later, and a Mixalune build exposing local API 1.0 with API Settings. The inspected SDK version is beta.19; compatibility with future host versions must be verified.

1. Download the `.macroDeckPlugin` file.
2. In Macro Deck, choose Integrations > Install from file and select the package.
3. In Mixalune, enable the local API and copy its authentication token.
4. Configure Mixalune Control with the matching port (default 18765) and token.
5. Add an action to a button and select a discovered channel.

## Controls

Set channel volume, increase/decrease volume, toggle/set channel mute, toggle microphone mute, reconnect, and set/raise/lower the microphone noise-gate threshold. The new gate actions require an updated Mixalune build advertising effects.setThreshold and effects.adjustThreshold. Includes dynamic channel discovery, custom names, stable saved IDs, event-driven button/variable feedback, secure token storage, and automatic reconnection without replaying old commands.

Game/stream and monitor volumes can be controlled separately; mute is shared. Percentages represent normalized fader travel, including boost, and can differ from native gain percentages.

## Development status

43 automated plugin tests passed and package validation was clean. Stub-host conformance and mock-server testing do not certify actual Macro Deck/Mixalune desktop integration or audio stability. Live application, sleep/resume, and audio-dropout testing remain pending. This unsigned development preview is not a Macro Deck Store release.

## Updates and distribution

Record new features and fixes under **Unreleased** in [CHANGELOG.md](CHANGELOG.md). For each release, add a dated version entry and update the compiled package, checksum, and setup instructions together.

The Macro Deck plugin is now open source under MIT; see [source and build instructions](source/README.md). The 0.2.0 download above is a local beta candidate using SDK beta.19. Version 0.1.2 installation and live controls were confirmed by the tester; 0.2.0 desktop testing remains pending. Mixalune application and Stream Deck source are not included.

The [existing 0.1.0 prerelease](https://github.com/knowledgezip/Mixalune/releases/tag/mixalune-control-v0.1.0) remains available. The [Elgato Stream Deck plugin](https://github.com/knowledgezip/Mixalune/tree/feature/mixalune-stream-deck) has its own separate branch and package format.

## Optional button icon pack

[Download Mixalune Icons 1.0.0](downloads/mixalune-icons-1.0.0.zip) — 24 PNG button icons plus a preview sheet, supplied by Knowledgezip Media. Extract the ZIP and select an image using Macro Deck's button appearance editor. This is a separate image pack, not an installable plugin. Images for routing, soundboard and EQ are artwork only; they do not add unsupported actions.
