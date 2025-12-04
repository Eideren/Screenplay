using System;
using Sirenix.Utilities.Editor;
using UnityEngine;
using YNode.Editor;
using Port = Screenplay.Nodes.Barriers.Port;

namespace Screenplay.Nodes.Editor.Barriers
{
    public class PortEditor<T> : NodeEditor, ICustomNodeEditor<Port>
    {
        public new Port Value => (Port)base.Value;

        public override int GetWidth() => Port.Width;

        public override GUIStyle GetBodyHighlightStyle() => GUIStyle.none;

        public override GUIStyle GetBodyStyle() => GUIStyle.none;

        public override void OnHeaderGUI()
        {

        }

        public override void OnBodyGUI()
        {
            var r = GUILayoutUtility.GetRect(Port.Width, Port.HeightOfPort);
            if (Window.LossyConnectedEditors.Contains(this))
                GUI.Box(r, EditorIcons.Link.ActiveGUIContent);

            if (UnityEngine.Event.current.type == EventType.Repaint)
                if (Array.IndexOf(Value.Parent.InheritedPorts, Value) == -1)
                    Window.RemoveNode(this, false);
        }
    }
}
