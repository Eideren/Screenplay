using System;
using System.Collections.Generic;
using System.Threading;
using Sirenix.OdinInspector;
using UnityEngine;
using YNode;
// Async method lacks 'await' operators and will run synchronously - done on purpose
#pragma warning disable CS1998

namespace Screenplay.Nodes
{
    [Serializable]
    public class Branch : AbstractScreenplayNode, IExecutable<IEventContext>, IPrerequisiteVisitedSelf
    {
        [Output, SerializeReference, Tooltip("What would run when Prerequisite is true")]
        public IExe<IEventContext>? True;

        [Output, SerializeReference, Tooltip("What would run when Prerequisite is false")]
        public IExe<IEventContext>? False;

        [Input(Stroke = NoodleStroke.Dashed), Required, SerializeReference, LabelWidth(20), HorizontalGroup(width:90), Tooltip("Select which action should be taken next")]
        public IPrerequisite Prerequisite = null!;

        public IEnumerable<IExe<IEventContext>> Followup()
        {
            if (True != null)
                yield return True;
            if (False != null)
                yield return False;
        }

        public Awaitable InnerExecution(IEventContext context, CancellationToken cancellation)
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

        public override void CollectReferences(List<GenericSceneObjectReference> references) { }
    }
}
