using Godot;
using System;

namespace ValeDasFlores;

public static class FarmAudio
{
    public const float EngineIdleDb = -22;
    public const float FootstepDb = -18;
    // Temporary dry-soil footsteps, synthesized locally; replace with recordings later.
    public static AudioStreamWav Footstep(int seed)
    {
        const int rate = 22050, samples = 6600;
        var random = new Random(seed);
        var data = new byte[samples * 2];
        float low = 0;
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / rate;
            float noise = (float)random.NextDouble() * 2 - 1;
            low = low * .65f + noise * .35f;
            float envelope = MathF.Min(t / .008f, 1) * MathF.Exp(-t * 23);
            float value = (low * .65f + MathF.Sin(t * 440) * MathF.Exp(-t * 35) * .3f) * envelope;
            short pcm = (short)(value * 23000);
            data[i * 2] = (byte)(pcm & 255);
            data[i * 2 + 1] = (byte)((pcm >> 8) & 255);
        }
        return new AudioStreamWav { Format = AudioStreamWav.FormatEnum.Format16Bits, MixRate = rate, Data = data };
    }
}
