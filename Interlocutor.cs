using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

namespace Screenplay
{
    [CreateAssetMenu(menuName = "Screenplay/Interlocutor")]
    public class Interlocutor : ScriptableObject, IInterlocutor
    {
        private static readonly SortedSet<SourceEndPair> s_sourcesPool = new();

        public float DefaultDuration = 0.1f;

        [SerializeField] private CharDuration[] _charDurations =
        {
            new()
            {
                Character = ' ',
                DurationInSeconds = 0f,
            },
            new()
            {
                Character = '\t',
                DurationInSeconds = 0f,
            },
            new()
            {
                Character = '\n',
                DurationInSeconds = 0f,
            },
            new()
            {
                Character = 'h',
                DurationInSeconds = 0f,
            },
            new()
            {
                Character = '.',
                DurationInSeconds = 1f,
            },
            new()
            {
                Character = ',',
                DurationInSeconds = 0.5f,
            },
            new()
            {
                Character = '!',
                DurationInSeconds = 1f,
            },
            new()
            {
                Character = '?',
                DurationInSeconds = 1f,
            }
        };

        public uint CharactersPerChatter = 4;
        public float Pitch = 1f;
        public float Volume = 1f;
        public AudioClip[] Chatter = Array.Empty<AudioClip>();
        public AudioMixerGroup? MixerGroup;

        private Dictionary<char, float> _charToDuration = null!;

        private void OnEnable()
        {
            _charToDuration = new();
            foreach (var charDuration in _charDurations)
                _charToDuration[charDuration.Character] = charDuration.DurationInSeconds;
        }

        public float GetDuration(char c)
        {
            if (_charToDuration.TryGetValue(c, out var duration) == false)
                if (_charToDuration.TryGetValue(char.IsLower(c) ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c), out duration) == false)
                    duration = DefaultDuration;

            return duration;
        }

        public async UniTask RunDialog(IEventContext context, IEnumerable<string> lines, bool previewMode, CancellationToken cancellation)
        {
            var ui = context.GetDialogUI();
            ui.StartDialogPresentation();
            foreach (var text in lines)
            {
                ui.StartLineTypewriting(text);
                ui.SetTypewritingCharacter(0);
                float time = 0f;
                int lastChatter = 0;
                for (int i = 0;
                     // Length - 1 as we don't want to delay after showing the last character
                     i < text.Length - 1 && ui.FastForwardRequested == false;
                     i++)
                {
                    ui.SetTypewritingCharacter(i + 1);

                    if (i - lastChatter >= CharactersPerChatter)
                        ChatterLogic(ref lastChatter, i, text);

                    time += GetDuration(text[i]);
                    for (; time > 0f && ui.FastForwardRequested == false; time -= Time.unscaledDeltaTime)
                        await UniTask.NextFrame(cancellation, cancelImmediately:true);
                }

                ChatterLogic(ref lastChatter, text.Length - 1, text);

                ui.SetTypewritingCharacter(text.Length);
                ui.FinishedTypewriting();

                while (previewMode || ui.DialogAdvancesAutomatically == false)
                {
                    if (ui.FastForwardRequested)
                    {
                        await UniTask.NextFrame(cancellation, cancelImmediately:true);
                        break;
                    }

                    await UniTask.NextFrame(cancellation, cancelImmediately:true);
                }
            }
            ui.EndDialogPresentation();
        }

        private void ChatterLogic(ref int last, int current, string text)
        {
            int hash = 0;
            int processed = 0;
            for (; last <= current; last++)
            {
                if (GetDuration(text[last]) == 0f)
                {
                    for (; last <= current && GetDuration(text[last]) == 0f; last++) { }
                    break;
                }

                hash = HashCode.Combine(hash, text[last]);
                processed++;
            }

            if (Chatter.Length == 0 || processed == 0)
                return;

            var index = hash % Chatter.Length;
            index = index < 0 ? Chatter.Length + index : index;
            var chatter = Chatter[index];
            PlayChatter(chatter);
        }

        private void PlayChatter(AudioClip chatter)
        {
            AudioSource source;
            if (s_sourcesPool.Count > 0 && s_sourcesPool.Min.End < Time.timeAsDouble)
            {
                source = s_sourcesPool.Min.Source;
                s_sourcesPool.Remove(s_sourcesPool.Min);
            }
            else
            {
                source = new GameObject($"{nameof(Interlocutor)}PooledAudio").AddComponent<AudioSource>();
            }

            source.spatialize = false;
            source.clip = chatter;
            source.pitch = Pitch;
            source.volume = Volume;
            source.outputAudioMixerGroup = MixerGroup;
            source.Play();
            s_sourcesPool.Add(new()
            {
                End = Time.timeAsDouble + chatter.length,
                Source = source
            });
        }

        private struct SourceEndPair : IComparable<SourceEndPair>, IEquatable<SourceEndPair>
        {
            public double End;
            public AudioSource Source;

            public int CompareTo(SourceEndPair other) => End.CompareTo(other.End);

            public bool Equals(SourceEndPair other) => End.Equals(other.End) && Source.Equals(other.Source);

            public override bool Equals(object? obj) => obj is SourceEndPair other && Equals(other);

            public override int GetHashCode() => HashCode.Combine(End, Source);
        }

        [Serializable]
        public struct CharDuration
        {
            public char Character;
            public float DurationInSeconds;
        }
    }
}
