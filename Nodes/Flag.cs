using System.Collections.Generic;
using System.Threading;
using Sirenix.OdinInspector;
using UnityEngine;
// Async method lacks 'await' operators and will run synchronously - done on purpose
#pragma warning disable CS1998

namespace Screenplay.Nodes
{
    public class Flag : Action
    {
        [HideLabel]
        public string Description = "Description";

        public override void CollectReferences(List<GenericSceneObjectReference> references){ }

        public override async Awaitable<IAction?> Execute(IContext context, CancellationToken cancellation)
        {
            return Next;
        }

        public override void FastForward(IContext context) { }

        public override void SetupPreview(IPreviewer previewer, bool fastForwarded) { }
    }
}
