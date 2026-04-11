using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using YNode;

namespace Screenplay.Nodes
{
    [Serializable, NodeVisuals(Icon = "Git")]
    public class Branch : AbstractScreenplayNode, IExecutable
    {
        /// <summary> What would run when Prerequisite is true </summary>
        [Output, SerializeReference]
        public IExecutable? True;

        /// <summary> What would run when Prerequisite is false </summary>
        [Output, SerializeReference]
        public IExecutable? False;

        [Input(Stroke = NoodleStroke.Dashed), SerializeReference, LabelWidth(20), HorizontalGroup(width:90), Tooltip("Select which action should be taken next")]
        public required IPrerequisite Prerequisite = null!;

        public IEnumerable<IExecutable?> Followup()
        {
            yield return True;
            yield return False;
        }

        public UniTask<IExecutable?> Execute(IEventContext context, Cancellation cancellation)
        {
            if (Prerequisite.TestPrerequisite(context))
                return new UniTask<IExecutable?>(True);
            else
                return new UniTask<IExecutable?>(False);
        }

        public UniTask Persistence(IEventContext context, Cancellation cancellation) => UniTask.CompletedTask;

        public void SetupPreview(IPreviewer previewer, bool fastForwarded)
        {
            if (fastForwarded == false)
                previewer.PlaySafeAction(this);
        }

        public override void CollectReferences(ReferenceCollector references) { }
    }
}
