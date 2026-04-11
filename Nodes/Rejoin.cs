using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using YNode;

namespace Screenplay.Nodes
{
    [Serializable, NodeVisuals(Icon = "UnityEditor.Graphs.AnimatorControllerTool@2x")]
    public class Rejoin : Bifurcate, IRejoin
    {
        public Rejoin()
        {
            Entries = new ExecutableEntry[1];
        }

        public void SetupPreview(IPreviewer previewer, bool fastForwarded) => throw new NotImplementedException();

        public UniTask<IExecutable?> Execute(IEventContext context, Cancellation cancellation) => throw new NotImplementedException();

        public UniTask Persistence(IEventContext context, Cancellation cancellation) => throw new NotImplementedException();
    }

    public interface IRejoin : IExecutable
    {
        UniTask<IExecutable?> IExecutable.Execute(IEventContext context, Cancellation cancellation) => throw new NotImplementedException();
        UniTask IExecutable.Persistence(IEventContext context, Cancellation cancellation) => throw new NotImplementedException();
        void IPreviewable.SetupPreview(IPreviewer previewer, bool fastForwarded) => throw new NotImplementedException();
    }
}
