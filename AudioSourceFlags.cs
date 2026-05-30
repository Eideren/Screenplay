using System;
using UnityEngine;

namespace Screenplay
{
    [Flags]
    public enum AudioSourceFlags
    {
        Loop = 0b000001,
        Mute = 0b000010,
        BypassEffects = 0b000100,
        BypassListenerEffects = 0b001000,
        BypassReverbZones = 0b010000,
        PlayOnAwake = 0b100000,
    }

    public static class AudioSourceFlagsExtension
    {
        public static void Apply(this AudioSourceFlags flags, AudioSource source)
        {
            source.SetFlags(flags);
        }

        public static void SetFlags(this AudioSource source, AudioSourceFlags flags)
        {
            source.loop = (AudioSourceFlags.Loop & flags) != 0;
            source.mute = (AudioSourceFlags.Mute & flags) != 0;
            source.bypassEffects = (AudioSourceFlags.BypassEffects & flags) != 0;
            source.bypassListenerEffects = (AudioSourceFlags.BypassListenerEffects & flags) != 0;
            source.bypassReverbZones = (AudioSourceFlags.BypassReverbZones & flags) != 0;
            source.playOnAwake = (AudioSourceFlags.PlayOnAwake & flags) != 0;
        }
    }
}
