# Installation

## Requirements

- Windows 10/11 x64 compatible with the installed Macro Deck 3 release.
- Macro Deck 3 beta.19 or newer; Macro Deck 2 is not supported.
- Mixalune with local API 1.0, including the API Settings interface.

Macro Deck supplies the .NET 10/ASP.NET Core runtime used by this framework-dependent plugin. No separate plugin service or Mixalune installation is bundled.

## Install the local package

In Macro Deck's Integrations page, use **Install from file…** to select
`com.knowledgezip.mixalune-control-0.1.2.macroDeckPlugin`. Review its identity and install it.
This is a local unsigned development artifact, not a store-approved release.

Do not rename it to a DLL or use Macro Deck 2's plugin-folder installation procedure. The official CLI assembles a manifest and `runtimes/win-x64` payload with file digests.

The installer API and menu label are confirmed in the current host source; the desktop installation flow has not been exercised on an installed host here.

## Enable Mixalune

In an API-enabled Mixalune build, open **API Settings** in the header, enable the local API and apply. Default port: 18765. Choose **Copy token**.

If your installed build has no API Settings button, it predates this API. Use a build containing the local-control implementation; installing this plugin cannot add the server to an older Mixalune executable.

Configure Mixalune Control in Macro Deck with the port and copied token. Setup tests the connection before saving. Add the actions described in [actions.md](actions.md).

## Uninstall

Remove or disable the integration using Macro Deck's plugin management. Its shutdown cancels pending work and disconnects from Mixalune. Mixalune remains running. Remove the saved integration configuration through the host if you also want its stored credential removed.


