using UnityEngine;
using YNode.Editor;
using Screenplay.Nodes;
using Event = Screenplay.Nodes.Event;

namespace Screenplay.Editor
{
    public class ScreenplayNodeEditor : CustomNodeEditor<AbstractScreenplayNode>
    {
        public override void OnHeaderGUI()
        {
            var thisScreenplay = (ScreenplayGraph)Graph;
            var textColor = GUI.color;
            var window = (ScreenplayEditor)Window;
            if (Value is IBranch && window.IsReachable(Value) == false)
                GUI.color = new Color(GUI.color.r, GUI.color.g * 0.25f, GUI.color.b * 0.25f, GUI.color.a);
            if (Value is IPrerequisite req && thisScreenplay.Visited(req))
                GUI.color = new Color(GUI.color.r * 0.25f, GUI.color.g, GUI.color.b * 0.25f, GUI.color.a);

            if (Value is Event e)
                DrawEditableTitle(ref e.Name);
            else
                base.OnHeaderGUI();
            GUI.color = textColor;
        }

        public override Color GetTint()
        {
            var baseTint = base.GetTint();
            var window = (ScreenplayEditor)Window;
            return window.InPreviewChain(Value) ? baseTint * new Color(1.2f, 1.2f, 1.5f, 1.2f) : baseTint;
        }
    }
}
