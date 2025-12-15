using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using YNode;

// Async method lacks 'await' operators and will run synchronously - done on purpose
#pragma warning disable CS1998

namespace Screenplay.Nodes.Unity
{
    [NodeVisuals(Icon = "d_Toggle Icon")]
    public class SetActive : ExecutableLinear
    {
        [HideLabel, HorizontalGroup] public required SceneObjectReference<GameObject> Target;
        [HideLabel, HorizontalGroup(width:16)] public bool Active = true;

        public override void CollectReferences(ReferenceCollector references) => references.Collect(Target);

        protected override async UniTask LinearExecution(IEventContext context, CancellationToken cancellation)
        {
            FastForward(context, cancellation);
        }

        public override void FastForward(IEventContext context, CancellationToken cancellationToken)
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
                    FastForward(previewer, CancellationToken.None);
                else
                    previewer.PlaySafeAction(this);
            }
        }
    }
}
