using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;

// Async method lacks 'await' operators and will run synchronously - done on purpose
#pragma warning disable CS1998

namespace Screenplay.Nodes
{
    public class Flag : ExecutableLinear
    {
        [HideLabel]
        public string Description = "Description";

        public override void CollectReferences(ReferenceCollector references){ }

        protected override async UniTask LinearExecution(IEventContext context, CancellationToken cancellation) { }

        public override UniTask Persistence(IEventContext context, CancellationToken cancellationToken) => UniTask.CompletedTask;

        public override void SetupPreview(IPreviewer previewer, bool fastForwarded) { }
    }
}
