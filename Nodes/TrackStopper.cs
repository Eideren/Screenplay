using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YNode;
// Async method lacks 'await' operators and will run synchronously - done on purpose
#pragma warning disable CS1998

namespace Screenplay.Nodes
{
    public class TrackStopper : ExecutableLinear
    {
        [Input, SerializeReference] public required TrackBackgroundPlayer? BackgroundPlayer;

        public override void CollectReferences(ReferenceCollector references) { }

        protected override async UniTask LinearExecution(IEventContext context, CancellationToken cancellation)
        {
            if (BackgroundPlayer is null)
            {
                Debug.LogWarning($"Unassigned {nameof(BackgroundPlayer)}, skipping this {nameof(TrackStopper)}");
                return;
            }

            Debug.LogError("Not implemented");
            //context.StopAsynchronous(BackgroundPlayer);
        }

        public override void FastForward(IEventContext context, CancellationToken cancellationToken)
        {
            if (BackgroundPlayer is null)
            {
                Debug.LogWarning($"Unassigned {nameof(BackgroundPlayer)}, skipping this {nameof(TrackStopper)}");
                return;
            }

            Debug.LogError("Not implemented");
            //context.StopAsynchronous(BackgroundPlayer);
        }

        public override void SetupPreview(IPreviewer previewer, bool fastForwarded)
        {
            if (BackgroundPlayer is null)
                return;

            Debug.LogError("Not implemented");
            //previewer.StopAsynchronous(BackgroundPlayer);
        }
    }
}
