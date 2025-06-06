using System.Collections.Generic;
using System.Threading;
using Sirenix.OdinInspector;
using UnityEngine;
using YNode;
// Async method lacks 'await' operators and will run synchronously - done on purpose
#pragma warning disable CS1998

namespace Screenplay.Nodes
{
    public class TrackStopper : Action
    {
        [Required, Input, SerializeReference] public TrackBackgroundPlayer? BackgroundPlayer;

        public override void CollectReferences(List<GenericSceneObjectReference> references) { }

        public override async Awaitable<IAction?> Execute(IContext context, CancellationToken cancellation)
        {
            if (BackgroundPlayer is null)
            {
                Debug.LogWarning($"Unassigned {nameof(BackgroundPlayer)}, skipping this {nameof(TrackStopper)}");
                return Next;
            }

            context.StopAsynchronous(BackgroundPlayer);
            return Next;
        }

        public override void FastForward(IContext context)
        {
            if (BackgroundPlayer is null)
            {
                Debug.LogWarning($"Unassigned {nameof(BackgroundPlayer)}, skipping this {nameof(TrackStopper)}");
                return;
            }

            context.StopAsynchronous(BackgroundPlayer);
        }

        public override void SetupPreview(IPreviewer previewer, bool fastForwarded)
        {
            if (BackgroundPlayer is null)
                return;

            previewer.StopAsynchronous(BackgroundPlayer);
        }
    }
}
