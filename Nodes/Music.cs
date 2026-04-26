using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using YNode;

namespace Screenplay.Nodes
{
    [NodeVisuals(Icon = "d_AudioImporter Icon")]
    public class Music : ExecutableLinear
    {
        public static AudioSource? CurrentPlaying;

        public required AudioClip Track;

        public float Volume = 1f;

        public bool Loop = true;

        [Unit(Units.Second)]
        public float TransitionDuration;

        /// <summary>
        /// Whether to start this track at the previous track's time when transitioning, useful to smoothly blend in multiple version of the same track
        /// </summary>
        public bool SynchronizedTransition;

        public override void CollectReferences(ReferenceCollector references) { }

        protected override UniTask LinearExecution(IEventContext context, Cancellation cancellation)
        {
            return UniTask.CompletedTask;
        }

        public override UniTask Persistence(IEventContext context, Cancellation cancellation)
        {
            float start = 0f;
            if (CurrentPlaying != null)
            {
                var previousPlaying = CurrentPlaying;
                if (SynchronizedTransition)
                    start = CurrentPlaying.time;

                CurrentPlaying = CurrentPlaying.gameObject.AddComponent<AudioSource>();
                TransitionOut(previousPlaying, TransitionDuration, cancellation).Forget();
            }
            else
            {
                var gObject = new GameObject(typeof(Music).FullName);
                gObject.hideFlags |= HideFlags.DontSave | HideFlags.DontUnloadUnusedAsset;
                CurrentPlaying = gObject.AddComponent<AudioSource>();
            }

            CurrentPlaying.clip = Track;
            CurrentPlaying.loop = Loop;
            CurrentPlaying.time = start;
            CurrentPlaying.Play();
            TransitionIn(CurrentPlaying, TransitionDuration, Volume, cancellation).Forget();

            return UniTask.CompletedTask;

            static async UniTask TransitionIn(AudioSource source, float duration, float volume, Cancellation cancellation)
            {
                try
                {
                    for (float t = 0; t < duration; await Uni.NextFrame(cancellation), t += Time.deltaTime)
                        source.volume = volume * (t / duration);

                    source.volume = volume;
                }
                catch // We destroy if cancellation was triggered
                {
                    Object.Destroy(source);
                    throw;
                }
            }
        }

        public static async UniTask TransitionOut(AudioSource source, float duration, Cancellation cancellation)
        {
            try
            {
                float initialVolume = source.volume;
                for (float t = 0; t < duration; await Uni.NextFrame(cancellation), t += Time.deltaTime)
                    source.volume = initialVolume * (1f - t / duration);

                source.volume = 0f;
            }
            finally // We destroy the source whether it was canceled or finished gracefully
            {
                Object.Destroy(source);
            }
        }

        public override void SetupPreview(IPreviewer previewer, bool fastForwarded)
        {

        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void Init()
        {
            CurrentPlaying = null;
        }
    }
}
