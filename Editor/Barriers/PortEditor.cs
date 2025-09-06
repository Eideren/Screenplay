using System;
using Screenplay.Nodes.Barriers;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using YNode.Editor;

namespace Screenplay.Nodes.Editor.Barriers
{
    public class PortEditor<T> : NodeEditor, ICustomNodeEditor<Port<T>> where T : IExecutableContext<T>
    {
        public new Port<T> Value => (Port<T>)base.Value;

        public override int GetWidth() => Port<T>.Width;

        public override GUIStyle GetBodyHighlightStyle() => GUIStyle.none;

        public override GUIStyle GetBodyStyle() => GUIStyle.none;

        public override void OnHeaderGUI()
        {

        }

        public override void OnBodyGUI()
        {
            var r = GUILayoutUtility.GetRect(Port<T>.Width, Port<T>.HeightOfPort);
            GUI.Box(r, EditorIcons.Link.ActiveGUIContent);

            if (UnityEngine.Event.current.type == EventType.Repaint)
                if (Array.IndexOf(Value.Parent.InheritedPorts, Value) == -1)
                    Window.RemoveNode(this, false);
        }
    }
}
