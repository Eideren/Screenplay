using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Screenplay.Component
{
    public class ScreenplayDispatcher : MonoBehaviour
    {
        private CancellationTokenSource? _existing;

        public required ScreenplayGraph Screenplay = null!;
        public uint Seed = 0;

        private ScreenplayGraph.IntrospectionKey _key = new();

        // ReSharper disable once Unity.IncorrectMethodSignature
        private async UniTask OnEnable()
        {
            if (transform.parent != null)
                transform.parent = null;

            DontDestroyOnLoad(gameObject);
            _existing = new CancellationTokenSource();
            await Screenplay.StartExecution(_existing.Token, Seed == 0 ? (uint)System.Diagnostics.Stopwatch.GetTimestamp() : Seed, _key);
        }

        private void OnDisable()
        {
            _existing?.Cancel();
            _existing?.Dispose();
            _existing = null;
        }

        public ScreenplayGraph.State SaveState()
        {
            var introspection = _key.BoundIntrospection;
            if (introspection is null)
                throw new InvalidOperationException("Screenplay was not started, nothing to save");

            return ScreenplayGraph.State.CreateFrom(introspection);
        }

        public void SetStateToLoad(ScreenplayGraph.State state)
        {
            var introspection = _key.BoundIntrospection;
            if (introspection is not null)
                throw new InvalidOperationException("Screenplay is running, too late to set the state to load");
            _key.StateToLoad = state;
        }

        private void OnDrawGizmos()
        {
            if (Screenplay == null)
                return;

            ScreenplayGizmos.Draw(Screenplay);
        }
    }
}
