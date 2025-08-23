using System.Collections.Generic;
using System.Threading;
using Sirenix.OdinInspector;
using UnityEngine;
// Async method lacks 'await' operators and will run synchronously - done on purpose
#pragma warning disable CS1998

namespace Screenplay.Nodes.Unity
{
    public class SetActive : ExecutableLinear
    {
        [Required, HideLabel, HorizontalGroup] public SceneObjectReference<GameObject> Target;
        [HideLabel, HorizontalGroup(width:16)] public bool Active = true;

        public override void CollectReferences(List<GenericSceneObjectReference> references) => references.Add(Target);

        protected override async Awaitable LinearExecution(IContext context, CancellationToken cancellation)
        {
            if (Target.TryGet(out var target, out _))
                target.SetActive(Active);
        }

        public override void FastForward(IContext context)
        {
            if (Target.TryGet(out var target, out _))
                target.SetActive(Active);
        }

        public override void SetupPreview(IPreviewer previewer, bool fastForwarded)
        {
            if (Target.TryGet(out var target, out _))
            {
                bool currentValue = target.activeSelf;
                previewer.RegisterRollback(() => target.SetActive(currentValue));
                if (fastForwarded)
                    FastForward(previewer);
                else
                    previewer.PlaySafeAction(this);
            }
        }
    }
}
