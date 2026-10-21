# Mixalune
Windows audio mixer beta releases from Knowledgezip Media.

## Mixalune Audio Driver

The in-development Windows virtual-audio driver source is under [`driver/MixaluneAudioDriver`](driver/MixaluneAudioDriver). It exposes **Mixalune Game Mix** and **Mixalune Virtual Microphone** and is derived from Microsoft's SysVAD sample.

The source and unsigned development packaging tools are published for review and testing. The driver is not a production-signed release and will not load on a normal Secure Boot system until Microsoft signs its catalog through the Windows Hardware Developer program.
