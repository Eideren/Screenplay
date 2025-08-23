using System;
using System.Collections.Generic;
using System.Threading;
using Sirenix.OdinInspector;
using UnityEngine;
using YNode;

namespace Screenplay.Nodes
{
    /// <summary> A node which does not branch, it only has one possible followup and will always continue onto that followup </summary>
    [Serializable]
    public abstract class ExecutableLinear : ScreenplayNode, IExecutable
    {
        [Output, SerializeReference, HideLabel, Tooltip("What would run right after this is done running")]
        public IExecutable? Next;

        public bool TestPrerequisite(HashSet<IPrerequisite> visited) => visited.Contains(this);

        public IEnumerable<IExecutable> Followup()
        {
            if (Next != null)
                yield return Next;
        }

        public async Awaitable InnerExecution(IContext context, CancellationToken cancellation)
        {
            await LinearExecution(context, cancellation);
            await Next.Execute(context, cancellation);
        }

        protected abstract Awaitable LinearExecution(IContext context, CancellationToken cancellation);

        public abstract void FastForward(IContext context);

        public abstract void SetupPreview(IPreviewer previewer, bool fastForwarded);
    }
}
