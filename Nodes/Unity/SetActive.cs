using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using Sirenix.OdinInspector;
using UnityEngine;
using YNode;

namespace Screenplay.Nodes.Unity
{
    [NodeVisuals(Icon = "d_Toggle Icon")]
    public class SetActive : ExecutableLinear
    {
        [HideLabel, HorizontalGroup] public required SceneObjectReference<GameObject> Target;
        [HideLabel, HorizontalGroup(width:16)] public bool Active = true;

        public override void CollectReferences(ReferenceCollector references) => references.Collect(Target);

        protected override UniTask LinearExecution(IEventContext context, CancellationToken cancellation)
        {
            return UniTask.CompletedTask; // Let persistence deal with this
        }

        public override async UniTask Persistence(IEventContext context, CancellationToken cancellationToken)
        {
            do
            {
                var go = await Target.GetAsync(cancellationToken);
                go.SetActive(Active);
                await go.OnDestroyAsync();
            } while (cancellationToken.IsCancellationRequested == false);
        }

        public override void SetupPreview(IPreviewer previewer, bool fastForwarded)
        {
            if (Target.TryGet(out var target, out _))
            {
                bool currentValue = target.activeSelf;
                previewer.RegisterRollback(() => target.SetActive(currentValue));
                if (fastForwarded)
                    target.SetActive(Active);
                else
                    previewer.PlaySafeAction(this);
            }
        }
    }
}
