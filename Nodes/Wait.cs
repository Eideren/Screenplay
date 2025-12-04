using System.Threading;
using Cysharp.Threading.Tasks;

namespace Screenplay.Nodes
{
    public class Wait : ExecutableLinear
    {
        public float Duration = 1f;

        public override void CollectReferences(ReferenceCollector references) { }

        protected override async UniTask LinearExecution(IEventContext context, CancellationToken cancellation)
        {
            await UniTask.WaitForSeconds(Duration, cancellationToken:cancellation, cancelImmediately:true);
        }

        public override void FastForward(IEventContext context, CancellationToken cancellationToken) { }

        public override void SetupPreview(IPreviewer previewer, bool fastForwarded) { }
    }
}
