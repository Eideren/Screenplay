using UnityEditor;
using UnityEngine;
using YNode.Editor;

namespace Screenplay.Editor
{
    public class PreconditionEditor : NodeEditor, ICustomNodeEditor<Precondition>
    {
        public new Precondition Value => (Precondition)base.Value;

        public override void OnHeaderGUI()
        {
            base.OnHeaderGUI();

            var bg = GUI.backgroundColor;
            var fg = GUI.backgroundColor;
            GUI.backgroundColor = new Color();
            var rect = GUILayoutUtility.GetLastRect();
            var thisScreenplay = (ScreenplayGraph)Graph;
            rect.x += rect.width - rect.height;
            rect.width = rect.height;

            foreach (var introspection in thisScreenplay.Introspections)
            {
                if (introspection.Preconditions.TryGetValue(Value, out var collectors))
                {
                    foreach (var collector in collectors)
                    {
                        GUI.color = collector.IsUnlocked ? Color.green : Color.red;
                        var icon = collector.IsUnlocked ? "Unlocked@2x" : "Locked@2x";
                        GUI.Box(rect, EditorGUIUtility.IconContent(icon).image);
                    }
                }
                else
                {
                    GUI.color = Color.red;
                    GUI.Box(rect, EditorGUIUtility.IconContent("d_Unlinked@2x").image);
                }
            }

            GUI.color = fg;
            GUI.backgroundColor = bg;
        }
    }
}
