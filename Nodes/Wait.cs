using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Screenplay.Nodes
{
    public class Wait : Action
    {
        public float Duration = 1f;

        public override void CollectReferences(List<GenericSceneObjectReference> references) { }

        public override async Awaitable<IAction?> Execute(IContext context, CancellationToken cancellation)
        {
            await Awaitable.WaitForSecondsAsync(Duration, cancellation);
            return Next;
        }

        public override void FastForward(IContext context) { }

        public override void SetupPreview(IPreviewer previewer, bool fastForwarded) { }
    }
}
