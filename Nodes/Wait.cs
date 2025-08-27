using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Screenplay.Nodes
{
    public class Wait : ExecutableLinear
    {
        public float Duration = 1f;

        public override void CollectReferences(List<GenericSceneObjectReference> references) { }

        protected override async Awaitable LinearExecution(IEventContext context, CancellationToken cancellation)
        {
            await Awaitable.WaitForSecondsAsync(Duration, cancellation);
        }

        public override void FastForward(IEventContext context, CancellationToken cancellationToken) { }

        public override void SetupPreview(IPreviewer previewer, bool fastForwarded) { }
    }
}
