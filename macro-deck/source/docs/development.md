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

## User-confirmed 0.2.0 desktop test (2026-10-24)

The tester explicitly confirmed installation of plugin 0.2.0 with the updated Mixalune build and successful noise-gate threshold button operation. This is user-reported desktop evidence, separate from automated tests. Long-duration audio stability, Windows sleep/resume, and clean-machine coverage remain unverified.

## Store submission (2026-10-25)

Creator Portal accepted trusted build 3 for version 0.2.0 from tag v0.2.0, commit 756a6d7af599701b8b19daff05b2560de16744f4, workflow run 40155173974. Windows conformance: 34 passed, 0 failed, 16 skipped. The release, summary, categories, manifest icon and two installed-host screenshots were submitted successfully. Portal status: In Review. Release automatically once approved is enabled. This is submission confirmation, not confirmation of public Store availability. The reviewer notes distinguish publisher-confirmed local package testing from CI stub-host testing.

The supplied Mixalune icon ZIP (24 PNG icons and preview) is published as a separate GitHub release asset and linked in the Store changelog. It is an extractable image ZIP, not a native .macroDeckIconPack Store project.

GitHub release: https://github.com/knowledgezip/Mixalune/releases/tag/v0.2.0
Creator Portal: https://creators.macro-deck.app/projects/com.knowledgezip.mixalune-control/versions

## 0.2.1 review corrections and installed validation (2026-10-26)

Source tag v0.2.1 is commit efa4434740695ffe40b3c3a290f72804568a9379. Trusted publish-plugin.yml run 40260680848 completed successfully with upload-artifact enabled; Creator Portal accepted build 4. Windows conformance suite: 34 passed, 0 failed, 16 skipped. Plugin automated suite: 43 passed.

The trusted workflow ZIP hash matched 632da17ee0f38c64f67df650fdbf03a53073ff6a061b5fc05967700e9e35d9d9. Its plugin package and SHA-256 checksum were attached to the GitHub v0.2.1 release. No compiled packages or dependency DLLs remain tracked in the current branch.

Installed that exact trusted package over 0.1.2 in Macro Deck beta.19. The host reported 0.2.1 Ready, Running cleanly, Fully compatible, 10 actions and 22 variables. Updated the local Mixalune application with the noise-gate API extension, preserving its prior files in a local backup. The plugin reconnected using saved credentials and reported API 1.0, four channels, and live threshold/enabled variables. Threshold Up 1 dB succeeded and the Mixalune UI reflected the increase. Threshold Down 1 dB succeeded, restoring the previous threshold. Mixer processing was resumed. This is installed-host evidence; long-duration listening, sleep/resume and clean-machine tests remain unverified.

Current screenshots are under docs/screenshots. Store approval remains pending.

Creator Portal resubmission confirmed on 2026-10-26: version 0.2.1 (build 4) is In Review, with automatic publication enabled after moderator approval. Three current installed-host screenshots and the wordmark-free manifest icon are uploaded. Updated October 26 Store Guidelines were accepted with the publisher's explicit authorization. This confirms submission, not public Store availability.
