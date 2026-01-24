using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
// Async method lacks 'await' operators and will run synchronously - done on purpose
#pragma warning disable CS1998

namespace Screenplay.Nodes
{
    public class TrackBackgroundPlayer : TrackPlayer
    {
        public bool Loop;

        private UniTask AsyncRunner(CancellationToken cancellation) => Track!.RangePlayer(GetTimeSpan(Track), cancellation, Loop);

        protected override async UniTask LinearExecution(IEventContext context, CancellationToken cancellation)
        {
            if (Track == null)
            {
                Debug.LogWarning($"Unassigned {nameof(Track)}, skipping this {nameof(TrackBackgroundPlayer)}");
                return;
            }

            Debug.LogError("Not implemented");
            //context.RunAsynchronously(this, AsyncRunner);
        }

        public override UniTask Persistence(IEventContext context, CancellationToken cancellationToken)
        {
            if (Track == null)
            {
                Debug.LogWarning($"Unassigned {nameof(Track)}, skipping this {nameof(TrackBackgroundPlayer)}");
                return UniTask.CompletedTask;
            }

            Debug.LogError("Not implemented");
            //context.RunAsynchronously(this, AsyncRunner);
            return UniTask.CompletedTask;
        }

        public override void SetupPreview(IPreviewer previewer, bool fastForwarded)
        {
            if (Track == null)
                return;

            foreach (var trackItem in Track.Items)
                trackItem?.AppendRollbackMechanism(previewer);

            Debug.LogError("Not implemented");
            //previewer.RunAsynchronously(this, AsyncRunner);
        }
    }
}
