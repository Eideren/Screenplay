using Screenplay.Nodes.Barriers;
using YNode.Editor;

namespace Screenplay.Nodes.Editor.Barriers
{
    public class BarrierIntermediateEditor : IBarrierPartEditor, ICustomNodeEditor<BarrierIntermediate>
    {
        public new BarrierIntermediate Value => (BarrierIntermediate)base.Value;

        public override void PreRemoval()
        {
            base.PreRemoval();

            foreach (var graphNode in Graph.Nodes)
            {
                if (graphNode is IBarrierPart barrierPart && barrierPart.NextBarrier == Value)
                {
                    barrierPart.NextBarrier = Value.NextBarrier;
                    barrierPart.NextBarrier?.UpdatePorts(barrierPart);
                }
            }

            if (Value.NextBarrier is not null)
                Window.ReplaceConnection(this, Window.NodesToEditor[Value.NextBarrier], true);
        }
    }
}
