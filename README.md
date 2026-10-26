# Mixalune Control 0.2.1

Macro Deck 3 plugin for Mixalune, prepared for Knowledgezip Media.
Windows x64; local control only; no cloud service, Mixalune bundle, account, administrator privileges, or embedded credentials.

**Development candidate, not yet certified for beta acceptance.** Automated WebSocket tests and Macro Deck's stub-host conformance suite are separate from testing the desktop applications together. See [development report](macro-deck/source/docs/development.md).

## Features

- Set channel volume, volume up/down, toggle/set mute, microphone mute, and reconnect.
- Set, raise, or lower the microphone noise-gate threshold in dB (requires the updated Mixalune API).
- Discovered channel IDs and custom display names; missing selections are preserved.
- Game/stream and monitor faders; shared mute.
- Secure token storage through Macro Deck's configuration secret API.
- Event-fed cache, button state providers, volume/mute variables.
- Reauthentication and state synchronization after a disconnect, without replaying commands.

The target is existing basic volume/mute functionality. The inspected Mixalune source returns `edition: null` and has no Base/Plus/Pro entitlement service; this plugin does not label it Base or attempt to enforce or bypass licensing. All commands are checked against capabilities.

## Start

1. Install Macro Deck 3 beta.19 or later on Windows 10/11 x64.
2. Use a Mixalune build with local control API 1.0 and its **API Settings** UI.
3. Obtain the plugin from the Macro Deck Store after approval, or the trusted workflow package attached to a [GitHub release](https://github.com/knowledgezip/Mixalune/releases). Install it using Integrations > Install from file.
4. Configure port and token; submitting setup tests the connection.
5. Add a Mixalune Control action to a Macro Deck button.

See [installation](macro-deck/source/docs/installation.md), [configuration](macro-deck/source/docs/configuration.md),
[actions and example buttons](macro-deck/source/docs/actions.md), and [troubleshooting](macro-deck/source/docs/troubleshooting.md).

**Volume percentages represent the normalized fader range.** 100% means the top of Mixalune's fader, including its boost range; it is not necessarily unity gain or the percentage printed in Mixalune's native UI.

## Build

Requires .NET SDK 10:

```powershell
cd macro-deck/source
dotnet restore Mixalune.Control.slnx
dotnet build Mixalune.Control.slnx -c Release --no-restore
dotnet test Mixalune.Control.slnx -c Release --no-build
dotnet tool install MacroDeck.Plugin.Cli --version 3.0.0-beta.19 --tool-path .tools
.tools/macrodeck-plugin build --source src/Mixalune.Control --output artifacts
.tools/macrodeck-plugin test --artifact artifacts/com.knowledgezip.mixalune-control-0.2.1.macroDeckPlugin
```

Distribution repository: [knowledgezip/Mixalune](https://github.com/knowledgezip/Mixalune). The Macro Deck plugin source is available under the MIT license in this branch. Mixalune itself is not included.
Store builds use the official Macro Deck reusable publishing workflow. Store availability requires review and approval.

## License and downloads

This plugin is MIT licensed; see [LICENSE](LICENSE). Mixalune application and Stream Deck source are not part of this branch. Dependencies restore through public NuGet references. Compiled plugin packages and dependency DLLs are not committed. GitHub release packages must be downloaded from the official trusted publishing workflow run, never substituted with a local build.

[Changelog](macro-deck/CHANGELOG.md) · [Optional icon ZIP](macro-deck/downloads/mixalune-icons-1.0.0.zip)
