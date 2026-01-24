using System.Threading;
using Cysharp.Threading.Tasks;
using YNode;

namespace Screenplay.Nodes
{
    [NodeVisuals(Icon = "UnityEditor.AnimationWindow")]
    public class Wait : ExecutableLinear
    {
        public float Duration = 1f;

        public override void CollectReferences(ReferenceCollector references) { }

        protected override async UniTask LinearExecution(IEventContext context, CancellationToken cancellation)
        {
            await UniTask.WaitForSeconds(Duration, cancellationToken:cancellation, cancelImmediately:true);
        }

        public override UniTask Persistence(IEventContext context, CancellationToken cancellationToken) => UniTask.CompletedTask;

        public override void SetupPreview(IPreviewer previewer, bool fastForwarded) { }
    }
}
