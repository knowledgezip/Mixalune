#include <sysvad.h>
#include "mixaluneaudiobridge.h"

SHORT MixaluneAudioBridge::s_samples[MixaluneAudioBridge::SampleCapacity] = {};
volatile LONG64 MixaluneAudioBridge::s_writePosition = 0;
volatile LONG64 MixaluneAudioBridge::s_readPosition = 0;

void MixaluneAudioBridge::PushStereoPcm16(_In_reads_bytes_(byteCount) const BYTE* data, ULONG byteCount)
{
    if (data == nullptr) return;

    const SHORT* frames = reinterpret_cast<const SHORT*>(data);
    const ULONG frameCount = byteCount / (sizeof(SHORT) * 2);
    LONG64 write = InterlockedCompareExchange64(&s_writePosition, 0, 0);
    const LONG64 read = InterlockedCompareExchange64(&s_readPosition, 0, 0);

    for (ULONG frame = 0; frame < frameCount; ++frame)
    {
        const LONG left = frames[frame * 2];
        const LONG right = frames[frame * 2 + 1];
        s_samples[static_cast<ULONG>(write % SampleCapacity)] = static_cast<SHORT>((left + right) / 2);
        ++write;
    }

    const LONG64 oldestAvailable = write - SampleCapacity;
    if (read < oldestAvailable) InterlockedExchange64(&s_readPosition, oldestAvailable);
    KeMemoryBarrier();
    InterlockedExchange64(&s_writePosition, write);
}

void MixaluneAudioBridge::PopMonoPcm16(_Out_writes_bytes_(byteCount) BYTE* data, ULONG byteCount)
{
    if (data == nullptr) return;

    SHORT* output = reinterpret_cast<SHORT*>(data);
    const ULONG sampleCount = byteCount / sizeof(SHORT);
    LONG64 read = InterlockedCompareExchange64(&s_readPosition, 0, 0);
    const LONG64 write = InterlockedCompareExchange64(&s_writePosition, 0, 0);

    for (ULONG sample = 0; sample < sampleCount; ++sample)
    {
        output[sample] = read < write ? s_samples[static_cast<ULONG>(read++ % SampleCapacity)] : 0;
    }
    if ((byteCount % sizeof(SHORT)) != 0) data[byteCount - 1] = 0;
    KeMemoryBarrier();
    InterlockedExchange64(&s_readPosition, read);
}

void MixaluneAudioBridge::Reset()
{
    InterlockedExchange64(&s_readPosition, 0);
    InterlockedExchange64(&s_writePosition, 0);
    RtlZeroMemory(s_samples, sizeof(s_samples));
}
