using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using YNode;

namespace Screenplay.Nodes
{
    /// <summary> A node which does not branch, it only has one possible followup and will always continue onto that followup </summary>
    [Serializable]
    public abstract class ExecutableLinear : AbstractScreenplayNode, IExecutable
    {
        [Output, SerializeReference, HideLabel, Tooltip("What would run right after this is done running")]
        public IExecutable? Next;

        public async UniTask<IExecutable?> InnerExecution(IEventContext context, CancellationToken cancellation)
        {
            await LinearExecution(context, cancellation);
            return Next;
        }

        protected abstract UniTask LinearExecution(IEventContext context, CancellationToken cancellation);

        public abstract UniTask Persistence(IEventContext context, CancellationToken cancellationToken);

        public abstract void SetupPreview(IPreviewer previewer, bool fastForwarded);

        IEnumerable<IExecutable?> IExecutable.Followup()
        {
            yield return Next;
        }
    }
}
