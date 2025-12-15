using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using YNode;
// Async method lacks 'await' operators and will run synchronously - done on purpose
#pragma warning disable CS1998

namespace Screenplay.Nodes
{
    [Serializable, NodeVisuals(Icon = "Git")]
    public class Branch : AbstractScreenplayNode, IExecutable
    {
        [Output, SerializeReference, Tooltip("What would run when Prerequisite is true")]
        public IExecutable? True;

        [Output, SerializeReference, Tooltip("What would run when Prerequisite is false")]
        public IExecutable? False;

        [Input(Stroke = NoodleStroke.Dashed), SerializeReference, LabelWidth(20), HorizontalGroup(width:90), Tooltip("Select which action should be taken next")]
        public required IPrerequisite Prerequisite = null!;

        public IEnumerable<IExecutable> Followup()
        {
            if (True != null)
                yield return True;
            if (False != null)
                yield return False;
        }

        public UniTask InnerExecution(IEventContext context, CancellationToken cancellation)
        {
            if (Prerequisite.TestPrerequisite(context))
                return True.Execute(context, cancellation);
            else
                return False.Execute(context, cancellation);
        }

        public void FastForward(IEventContext context, CancellationToken cancellationToken) { }

        public void SetupPreview(IPreviewer previewer, bool fastForwarded)
        {
            if (fastForwarded == false)
                previewer.PlaySafeAction(this);
        }

        public override void CollectReferences(ReferenceCollector references) { }
    }
}
