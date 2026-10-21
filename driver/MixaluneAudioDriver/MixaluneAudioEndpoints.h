#pragma once

// Stable public identity for the Mixalune virtual audio pair.
#define MIXALUNE_AUDIO_RENDER_ENDPOINT_NAME       L"Mixalune Game Mix"
#define MIXALUNE_AUDIO_CAPTURE_ENDPOINT_NAME      L"Mixalune Virtual Microphone"
#define MIXALUNE_AUDIO_ADAPTER_FRIENDLY_NAME      L"Mixalune Virtual Audio Adapter"
#define MIXALUNE_AUDIO_ROOT_HARDWARE_ID           L"Root\\MixaluneAudioDriver"

// Do not change after deployment. New GUIDs require endpoint migration.
// {5871EFDB-B04E-45BB-9C2D-E9EC7648A8ED}
DEFINE_GUID(MIXALUNE_AUDIO_RENDER_COMPONENT_GUID,
0x5871efdb, 0xb04e, 0x45bb, 0x9c, 0x2d, 0xe9, 0xec, 0x76, 0x48, 0xa8, 0xed);

// {D51E0844-348D-4B6D-9E1C-A2D045A401E8}
DEFINE_GUID(MIXALUNE_AUDIO_CAPTURE_COMPONENT_GUID,
0xd51e0844, 0x348d, 0x4b6d, 0x9e, 0x1c, 0xa2, 0xd0, 0x45, 0xa4, 0x01, 0xe8);
