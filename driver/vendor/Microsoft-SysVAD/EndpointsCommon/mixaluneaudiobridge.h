#pragma once

// Lock-free, single-producer/single-consumer transport used by the Mixalune Audio
// render and capture miniports. Audio is stored as 48 kHz, signed 16-bit mono.
class MixaluneAudioBridge final
{
public:
    static void PushStereoPcm16(_In_reads_bytes_(byteCount) const BYTE* data, ULONG byteCount);
    static void PopMonoPcm16(_Out_writes_bytes_(byteCount) BYTE* data, ULONG byteCount);
    static void Reset();

private:
    static constexpr ULONG SampleCapacity = 48000;
    static SHORT s_samples[SampleCapacity];
    static volatile LONG64 s_writePosition;
    static volatile LONG64 s_readPosition;
};
