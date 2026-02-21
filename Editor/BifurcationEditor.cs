using System.Linq;
using YNode.Editor;
using Screenplay.Nodes;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using YNode;

namespace Screenplay.Editor
{
    public class BifurcateEditor : NodeEditor, ICustomNodeEditor<Bifurcate>
    {
        private static readonly string ValueEntries = $"{nameof(NodeEditor.Value)}.{nameof(Bifurcate.Entries)}";

        public new Bifurcate Value => (Bifurcate)base.Value;

        public override int GetWidth() => 40;

        public override void OnHeaderGUI()
        {
            if (Utilities.GetAttrib<NodeVisualsAttribute>(Value.GetType(), out var visualsAttrib) && visualsAttrib.Icon is { } iconPath)
            {
                var r = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.Height(TitleHeight));
                r.x += (r.width - r.height) * 0.5f;
                r.width = r.height;
                GUI.tooltip = Value.GetType().Name;
                GUI.DrawTexture(r, EditorGUIUtility.IconContent(iconPath).image);

                AddCursorRectFromBody(r, MouseCursor.Pan);
            }
        }

        public override void OnBodyGUI()
        {
            try
            {
                ObjectTree.BeginDraw(true);

                GUIHelper.PushLabelWidth(84);

                var add = EditorGUIUtility.IconContent("CollabCreate Icon").image;
                var remove = EditorGUIUtility.IconContent("CollabDeleted Icon").image;
                var warn = EditorGUIUtility.IconContent("console.erroricon@2x").image;

                var entriesDrawer = ObjectTree.GetPropertyAtPath(ValueEntries).Children;
                for (int i = 0; i < Value.Entries.Length; i++)
                {
                    entriesDrawer[i].Children[0].Draw(GUIContent.none);
                    var lastRect = GUILayoutUtility.GetLastRect();
                    var removeButtonRect = lastRect;
                    if (Value.Entries[i].Executable == null!)
                    {
                        lastRect.height -= EditorGUIUtility.singleLineHeight;
                        lastRect.width = GetWidth();
                        lastRect.x = 0;
                        GUI.Box(lastRect, new GUIContent(warn, "Entry is not connected"), SirenixGUIStyles.Button);
                    }

                    removeButtonRect.width = GetWidth();
                    removeButtonRect.y += removeButtonRect.height - EditorGUIUtility.singleLineHeight;
                    removeButtonRect.height = EditorGUIUtility.singleLineHeight;
                    removeButtonRect.x = 0;
                    if (GUI.Button(removeButtonRect, remove, SirenixGUIStyles.TitleCentered))
                    {
                        GUI.changed = true;
                        var entries = Value.Entries.ToList();
                        entries.RemoveAt(i);
                        Value.Entries = entries.ToArray();
                    }
                }

                var addButtonRect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth, EditorGUIUtility.singleLineHeight);
                addButtonRect.width = GetWidth();
                addButtonRect.x = 0;
                if (GUI.Button(addButtonRect, add, SirenixGUIStyles.TitleCentered))
                {
                    GUI.changed = true;
                    var entries = Value.Entries.ToList();
                    entries.Add(default);
                    Value.Entries = entries.ToArray();
                }

                GUIHelper.PopLabelWidth();

                // Only comply with repaint requests if the editor has visual focus
                if (GUIHelper.RepaintRequested)
                {
                    GUIHelper.ClearRepaintRequest();
                    if (Window.HoveredNode == this)
                        Window.Repaint();
                }
            }
            finally
            {
                ObjectTree.EndDraw();
            }
        }
    }
}
