using UnityEngine;
using YNode;

namespace Screenplay.Nodes.Logic
{
    [NodeWidth(100)]
    public class Not : AbstractScreenplayNode, IPrerequisite
    {
        [Input(Stroke = NoodleStroke.Dashed), SerializeReference]
        public required IPrerequisite A = null!;

        public bool TestPrerequisite(IEventContext context) => A.TestPrerequisite(context) == false;

        public override void CollectReferences(ReferenceCollector references)
        {
            A?.CollectReferences(references);
        }
    }
}
