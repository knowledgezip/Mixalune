# Mixalune Control 0.1.1

Macro Deck 3 plugin for Mixalune, prepared for Knowledgezip Media.
Windows x64; local control only; no cloud service, Mixalune bundle, account, administrator privileges, or embedded credentials.

**Development candidate, not yet certified for beta acceptance.** Automated WebSocket tests and Macro Deck's stub-host conformance suite are separate from testing the desktop applications together. See [development report](docs/development.md).

## Features

- Set channel volume, volume up/down, toggle/set mute, microphone mute, and reconnect.
- Discovered channel IDs and custom display names; missing selections are preserved.
- Game/stream and monitor faders; shared mute.
- Secure token storage through Macro Deck's configuration secret API.
- Event-fed cache, button state providers, volume/mute variables.
- Reauthentication and state synchronization after a disconnect, without replaying commands.

The target is existing basic volume/mute functionality. The inspected Mixalune source returns `edition: null` and has no Base/Plus/Pro entitlement service; this plugin does not label it Base or attempt to enforce or bypass licensing. All commands are checked against capabilities.

## Start

1. Install Macro Deck 3 beta.19 or later on Windows 10/11 x64.
2. Use a Mixalune build with local control API 1.0 and its **API Settings** UI.
3. Install `com.knowledgezip.mixalune-control-0.1.1.macroDeckPlugin`.
4. Configure port and token; submitting setup tests the connection.
5. Add a Mixalune Control action to a Macro Deck button.

See [installation](docs/installation.md), [configuration](docs/configuration.md),
[actions and example buttons](docs/actions.md), and [troubleshooting](docs/troubleshooting.md).

**Volume percentages represent the normalized fader range.** 100% means the top of Mixalune's fader, including its boost range; it is not necessarily unity gain or the percentage printed in Mixalune's native UI.

## Build

Requires .NET SDK 10:

```powershell
dotnet restore Mixalune.Control.slnx
dotnet build Mixalune.Control.slnx -c Release --no-restore
dotnet test Mixalune.Control.slnx -c Release --no-build
dotnet tool install MacroDeck.Plugin.Cli --version 3.0.0-beta.19 --tool-path .tools
.tools/macrodeck-plugin build --source src/Mixalune.Control --output artifacts
.tools/macrodeck-plugin test --artifact artifacts/com.knowledgezip.mixalune-control-0.1.1.macroDeckPlugin
```

Distribution repository: [knowledgezip/Mixalune](https://github.com/knowledgezip/Mixalune). The Macro Deck plugin source is available under the MIT license in this branch. Mixalune itself is not included.
Store builds use the official Macro Deck reusable publishing workflow. Store availability requires review and approval.



