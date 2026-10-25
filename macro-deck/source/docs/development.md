# Development

Requires .NET SDK 10. The solution is self-contained and does not reference the Mixalune application source. SDK packages and CLI are pinned to 3.0.0-beta.19.

Run the build, tests, and packaging commands in the README from this source directory. The plugin targets Windows x64 and uses Macro Deck secure configuration storage for the API token. Never commit user settings or tokens.

## Architecture and API

MixaluneApiClient handles authenticated WebSocket transport, request correlation, bounded JSON parsing and timeouts. MixaluneConnectionManager owns a single reconnecting session with heartbeat and cancellation. MixaluneStateManager maintains the event-fed cache. MixaluneActionManager exposes ten actions with dynamic channel selectors and capability checks. MixaluneSettingsManager supplies the configuration flow. MixaluneIntegration supplies host lifecycle and variables.

The endpoint is ws://127.0.0.1:18765/api/v1 (port configurable); API version 1.0. Authentication is the first auth.authenticate command with the token in params. The client uses system.getVersion, system.getCapabilities, system.getState, system.ping, channel.list, channel.setVolume, channel.adjustVolume, channel.setMute, channel.toggleMute, microphone.toggleMute, and events.subscribe. Subscription categories are channel, microphone, system and effects. NoiseGateAction uses effects.setThreshold and effects.adjustThreshold only when advertised. Reconnection authenticates and resynchronizes; it never replays control commands.

Channels use stable IDs. Missing selections remain unavailable. Percentage values represent normalized fader travel, including boost, rather than unity gain. Edition can be unreported. The plugin only exposes advertised capabilities and does not implement licensing.

## Verification for 0.1.2

2026-10-24: Release build and 32 automated tests passed on Windows with SDK beta.19. These include loopback WebSocket and SDK harness tests, not full desktop/audio acceptance. Packaged-plugin conformance suite 1.3.0 passed: 34 passed, 0 failed, 16 skipped. Representative installed-host checks must pass before release submission. Record their actual results here; do not infer them from mocks.

## Publication

Only this Macro Deck plugin is MIT licensed. The repository workflow uses the official reusable Macro Deck publishing workflow, which builds, tests, runs conformance and uploads a trusted build. Connect the repository to the Creator Portal project before publishing a version tag/release from feature/mixalune-macro-deck. The Portal still requires a reviewed listing and truthful manual-test confirmation; a GitHub release alone is not Store publication.

Update ../CHANGELOG.md under Unreleased, then move entries to a dated release heading when shipping. The manifest AI disclosure covers AI-assisted shipped assets/text; the plugin performs no AI interaction and sends no data to an AI service.



## Installed-host verification (2026-10-24)

Macro Deck 3.0.0-beta.19 on Windows loaded plugin 0.1.2 and displayed Ready, seven actions and 20 variables. The channel dropdown discovered Microphone, App Audio, Browser and Desktop. The tester confirmed successful installation, authenticated connection, and working live controls. These user-reported checks are separate from automated tests; extended audio stability, sleep/resume and clean-machine testing remain unverified.



## Verification for 0.2.0 (2026-10-24)

43 plugin automated tests passed, including threshold set/up/down, boundary clamping, invalid values rejected before transmission, older-server compatibility, effect events, variables, and reconnection synchronization. Official packaged-plugin conformance suite 1.3.0: 34 passed, 0 failed, 16 skipped. A separate private Mixalune test run passed 64 tests, including the real authenticated loopback API's threshold commands, model mutation and native change events. The updated Windows x64 application and plugin package both built successfully. Existing NAudio deprecation warnings remain in the application build.

No new dependencies. Version 0.2.0 still requires desktop-host and audio acceptance testing. The previous tester confirmation applies to 0.1.2 only. Application source and private API implementation tests are not part of this public plugin repository.
