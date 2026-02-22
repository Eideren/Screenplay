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
            if (Value is IRejoin)
            {
                var lastRect = GUILayoutUtility.GetLastRect();
                var timeRect = lastRect;

                timeRect.y += timeRect.height - 10;
                timeRect.y = CachedSize.y - 20;
                timeRect.width = timeRect.height = 24;
                timeRect.x = 0;

                GUI.Box(timeRect, _waitContent, SirenixGUIStyles.None);

                GUIHelper.PushColor(new Color(1, 1, 1, 0.5f));
                GUI.DrawTexture(new Rect(0, 6, 2, CachedSize.y-12), Texture2D.whiteTexture);
                GUIHelper.PopColor();
            }

            try
            {
                ObjectTree.BeginDraw(true);

                GUIHelper.PushLabelWidth(84);

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
                        GUI.Box(lastRect, _warnEntryNotConnected, SirenixGUIStyles.Button);
                    }

                    removeButtonRect.y += removeButtonRect.height - EditorGUIUtility.singleLineHeight;
                    removeButtonRect.height = EditorGUIUtility.singleLineHeight;
                    removeButtonRect.width = removeButtonRect.height;
                    removeButtonRect.x = GetWidth() / 2f - removeButtonRect.width / 2f;

                    if (GUI.Button(removeButtonRect, _remove, SirenixGUIStyles.None))
                    {
                        GUI.changed = true;
                        var entries = Value.Entries.ToList();
                        entries.RemoveAt(i);
                        Value.Entries = entries.ToArray();
                    }
                }

                var addButtonRect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth, EditorGUIUtility.singleLineHeight);
                addButtonRect.width = addButtonRect.height;
                addButtonRect.x = GetWidth() / 2f - addButtonRect.width / 2f;
                if (GUI.Button(addButtonRect, _add, SirenixGUIStyles.None))
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

        private static readonly string ValueEntries = $"{nameof(NodeEditor.Value)}.{nameof(Bifurcate.Entries)}";
        private static GUIContent? __warnEntryNotConnected;
        private static GUIContent? __waitContent;
        private static Texture? __add;
        private static Texture? __remove;
        private static GUIContent _warnEntryNotConnected => __warnEntryNotConnected ??= new GUIContent(EditorGUIUtility.IconContent("console.erroricon@2x").image, "Entry is not connected");
        private static GUIContent _waitContent => __waitContent ??= new GUIContent(EditorGUIUtility.IconContent("UnityEditor.AnimationWindow").image, "Any path reaching this node will block until all of them reached it");
        private static Texture _add => __add ??= EditorGUIUtility.IconContent("CollabCreate Icon").image;
        private static Texture _remove => __remove ?? EditorGUIUtility.IconContent("CollabDeleted Icon").image;
    }
}
