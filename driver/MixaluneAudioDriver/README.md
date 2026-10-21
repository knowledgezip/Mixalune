# Mixalune Audio Driver

This directory contains the in-development native endpoint pair:

- **Mixalune Game Mix** — render endpoint written by Mixalune.
- **Mixalune Virtual Microphone** — capture endpoint selected in a game or chat application.

The driver is a root-enumerated x64 WaveRT virtual audio adapter derived from Microsoft's SysVAD sample. Its render miniport writes 48 kHz PCM16 stereo frames to a nonpaged circular buffer; the capture miniport reads 48 kHz PCM16 mono frames, emits silence on underrun, and drops the oldest frames on overrun.

## Current state

The vendored SysVAD source is reduced to one render/capture pair and uses the public hardware identity `Root\MixaluneAudioDriver`. This is development source, not a signed production release.

## Build requirements

1. Visual Studio with the Desktop C++ workload.
2. A complete matching Windows Driver Kit, including `km\portcls.h`.
3. PowerShell on Windows x64.

Build the native projects with `.\build-driver.ps1 -Configuration Release`.
Create an unsigned, signability-checked package with `.\package-driver.ps1 -Configuration Release`.
For isolated development testing, pass `-Thumbprint <certificate-thumbprint>` explicitly.

## Signing boundary

No certificate, private key, certificate password, or machine-specific thumbprint is stored in this repository. Secure Boot systems require a Microsoft-signed catalog. Do not ask ordinary users to disable Secure Boot or leave Windows test-signing enabled.

`build-attestation-cab.ps1` converts the Release package and matching PDB into the CAB layout used for Microsoft Hardware Dashboard submission. See `ATTESTATION-SUBMISSION.md` for the remaining external signing and validation gates.

Before distribution, validate render-to-capture audio, underrun and overrun behavior, sleep/resume, device removal, endpoint enumeration, Driver Verifier, and the applicable audio HLK playlist.
