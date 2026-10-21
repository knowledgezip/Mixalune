# Mixalune Audio Driver attestation submission

## Prepared locally

- x64 Release kernel driver and matching PDB.
- `Root\MixaluneAudioDriver` INF with the Mixalune Audio render and capture endpoint identities.
- Signability-tested catalog.
- Submission manifest containing byte sizes and SHA-256 hashes.
- CAB layout with one architecture and a short, plain folder name: `MixaluneAudioDriver`.

Run `build-attestation-cab.ps1` after any driver source change. The result is `attestation\MixaluneAudioDriverAttestation.cab`.

## External prerequisites

1. Obtain an EV code-signing certificate from a Microsoft-supported certificate authority.
2. Register the legal organization and EV certificate with Microsoft Partner Center's Hardware Developer Program.
3. Sign `MixaluneAudioDriverAttestation.cab` with the certificate registered to that Partner Center account using SHA-256. Do not submit the locally test-signed CAB as if it were EV signed.
4. Create a new x64 Windows 10/11 desktop driver submission and upload the EV-signed CAB.
5. Download Microsoft's returned package. Microsoft regenerates and replaces the submitted catalog.
6. Verify the returned Microsoft signature before installing it on a Secure Boot system.

## Validation still required before distribution

- Install and exercise the Microsoft-signed package on a separate Secure Boot Windows 11 machine.
- Verify render-to-capture audio, underrun silence, overrun recovery, sleep/resume, device removal, and Discord/game endpoint enumeration.
- Run Driver Verifier only on a disposable test machine with recovery access.
- For production distribution or Windows Update publication, complete the applicable HLK playlist and submit the HLK results instead of treating attestation as certification.

## Current boundary

The local certificate proves package integrity only on the development machine. It is not a substitute for Microsoft signing and cannot load this new kernel driver while Secure Boot remains enabled.
