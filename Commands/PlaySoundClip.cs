using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

namespace Screenplay.Commands
{
    [Serializable] public class PlaySoundClip : ICommand
    {
        public bool BlockWhilePlaying = false;
        public bool BypassEffects = false;
        public AudioClip Clip;
        public AudioMixerGroup MixerGroup;
        public float Volume = 1f;

        public void ValidateSelf()
        {
            if (Clip == null)
                throw new NullReferenceException("Clip");
            if (MixerGroup == null)
                throw new NullReferenceException("MixerGroup");
        }

        public IEnumerable<(string name, IValidatable validatable)> GetSubValues() => this.NoSubValues();

        public string GetInspectorString() => $"Play '{(Clip == null ? "null" : Clip.name)}'";

        public virtual IEnumerable Run(Stage stage)
        {
            AudioSource source = new GameObject(GetType().ToString()).AddComponent<AudioSource>();
            ApplyProperties(source);
            source.Play();
            if (BlockWhilePlaying)
            {
                while (source.isPlaying)
                    yield return null;
            }

            Object.Destroy(source);
        }

        protected void ApplyProperties(AudioSource source)
        {
            source.clip = Clip;
            source.outputAudioMixerGroup = MixerGroup;
            source.volume = Volume;
            source.spatialize = false;
            source.bypassEffects = BypassEffects;
        }
    }
}