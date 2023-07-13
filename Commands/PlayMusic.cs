using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Screenplay.Commands
{
    public class PlayMusic : PlaySoundClip
    {
        static readonly List<MusicState> MusicPlayersIdle = new();
        static MusicController _musicController;

        public float CrossfadeDuration = 1f;
        public bool Loop = true;

        public override IEnumerable Run(Stage stage)
        {
            AudioSource source = null;
            for (int i = 0; i < MusicPlayersIdle.Count; i++)
            {
                MusicState thisMusic = MusicPlayersIdle[i];
                if (thisMusic.FadingPerSecond > 0f)
                {
                    thisMusic.FadingPerSecond = 0f - MusicPlayersIdle[i].Source.volume / CrossfadeDuration;
                    MusicPlayersIdle[i] = thisMusic;
                }
                else if (thisMusic.Source.volume <= 0f)
                {
                    source = thisMusic.Source;
                    MusicPlayersIdle[i] = MusicPlayersIdle[^1];
                    MusicPlayersIdle.RemoveAt(MusicPlayersIdle.Count);
                }
            }

            if (source == null)
            {
                if (_musicController == null)
                    _musicController = new GameObject("Music Player").AddComponent<MusicController>();

                source = _musicController.gameObject.AddComponent<AudioSource>();
                MusicPlayersIdle.Add(new MusicState
                {
                    Source = source,
                    FadingPerSecond = 1f / CrossfadeDuration
                });
            }

            source.Stop();
            ApplyProperties(source);
            source.loop = Loop;
            source.Play();
            if (BlockWhilePlaying)
            {
                while (source.isPlaying)
                    yield return null;
            }
        }

        struct MusicState
        {
            public AudioSource Source;
            public float FadingPerSecond;
        }

        public class MusicController : MonoBehaviour
        {
            void Update()
            {
                foreach (MusicState state in MusicPlayersIdle)
                {
                    float volume = state.Source.volume;
                    if ((volume <= 0f && state.FadingPerSecond <= 0f) || (volume >= 1f && state.FadingPerSecond >= 0f))
                        continue;

                    volume += state.FadingPerSecond;
                    if (volume <= 0f)
                    {
                        state.Source.Stop();
                        state.Source.volume = 0f;
                    }
                    else
                        state.Source.volume = Mathf.Min(volume, 1f);
                }
            }
        }
    }
}