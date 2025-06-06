﻿using System;
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
    public class Branch : ScreenplayNode, IAction
    {
        [Output, SerializeReference, Tooltip("What would run when Prerequisite is true")]
        public IAction? True;

        [Output, SerializeReference, Tooltip("What would run when Prerequisite is false")]
        public IAction? False;

        [Input(Stroke = NoodleStroke.Dashed), Required, SerializeReference, LabelWidth(20), HorizontalGroup(width:90), Tooltip("Select which action should be taken next")]
        public IPrerequisite Prerequisite = null!;

        public bool TestPrerequisite(HashSet<IPrerequisite> visited) => visited.Contains(this);

        public IEnumerable<IAction> Followup()
        {
            if (True != null)
                yield return True;
            if (False != null)
                yield return False;
        }

        public async Awaitable<IAction?> Execute(IContext context, CancellationToken cancellation)
        {
            if (Prerequisite.TestPrerequisite(context.Visited))
                return True;
            else
                return False;
        }

        public void FastForward(IContext context) { }

        public void SetupPreview(IPreviewer previewer, bool fastForwarded)
        {
            if (fastForwarded == false)
                previewer.PlaySafeAction(this);
        }

        public override void CollectReferences(List<GenericSceneObjectReference> references) { }
    }
}
