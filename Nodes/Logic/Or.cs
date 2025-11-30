using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using YNode;

namespace Screenplay.Nodes.Logic
{
    [NodeWidth(100)]
    public class Or : AbstractScreenplayNode, IPrerequisite
    {
        [Input(Stroke = NoodleStroke.Dashed), SerializeReference]
        public required IPrerequisite A = null!, B = null!;

        public bool TestPrerequisite(IEventContext context) => A.TestPrerequisite(context) || B.TestPrerequisite(context);

        public override void CollectReferences(ReferenceCollector references)
        {
            A?.CollectReferences(references);
            B?.CollectReferences(references);
        }
    }
}
