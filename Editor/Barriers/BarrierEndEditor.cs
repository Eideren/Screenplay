using Screenplay.Nodes.Barriers;
using UnityEngine;
using YNode.Editor;

namespace Screenplay.Nodes.Editor.Barriers
{
    public class BarrierEndEditor : IBarrierPartEditor, ICustomNodeEditor<BarrierEnd>
    {
        public new BarrierEnd Value => (BarrierEnd)base.Value;

        public override void OnBodyGUI()
        {
            GUILayout.Space(20);
            base.OnBodyGUI();
            var lastR = GUILayoutUtility.GetLastRect();
            var inheritedPortLength = (Value.InheritedPorts.Length * Port<IEventContext>.HeightOfPort) + Port<IEventContext>.OffsetFromTop;
            var heightLeft = inheritedPortLength - lastR.y + lastR.height;

            GUILayoutUtility.GetRect(0, 0, heightLeft, heightLeft);
        }

        public override void PreRemoval()
        {
            base.PreRemoval();

            for (int i = Graph.Nodes.Count - 1; i >= 0; i--)
            {
                if (Graph.Nodes[i] is Barrier barrier && barrier.End() == Value)
                    Window.RemoveNode(Window.NodesToEditor[barrier], true);
            }
        }
    }
}
