using System;
using System.Collections.Generic;
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
        private List<Rect> _connectedToPortsRect = new();
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

        private static readonly HashSet<object> _recursiveCutoff = new();

        public void DrawExtension()
        {
            _connectedToPortsRect.Clear();
            if (ActivePorts.Count <= 1)
                return;

            GUIHelper.PushColor(new Color(0, 0, 0, 0.1f));
            foreach (var (_, port) in ActivePorts)
            {
                RecursiveRectGrow(port, out var r);
                r.xMin = Value.Position.x + CachedSize.x / 2f;
                _connectedToPortsRect.Add(r);
                r = Window.GridToWindowRect(r);
                GUI.DrawTexture(r, Texture2D.whiteTexture);
            }
            GUIHelper.PopColor();

            static void RecursiveRectGrow(Port port, out Rect r)
            {
                _recursiveCutoff.Clear();
                r = default;
                while (port.ConnectedEditor is not null)
                {
                    if (_recursiveCutoff.Add(port.ConnectedEditor) == false)
                        return;

                    if (port.Connected is IRejoin)
                    {
                        r.xMax = port.Connected.Position.x;
                        return;
                    }

                    var editorRect = new Rect(port.ConnectedEditor.Value.Position, port.ConnectedEditor.CachedSize);
                    if (r.size == default)
                    {
                        r = editorRect;
                    }
                    else
                    {
                        r.min = Vector2.Min(r.min, editorRect.min);
                        r.max = Vector2.Max(r.max, editorRect.max);
                    }

                    bool found = false;
                    foreach (var (_, nextPort) in port.ConnectedEditor.ActivePorts)
                    {
                        if (nextPort.Direction == IO.Output)
                        {
                            found = true;
                            port = nextPort;
                            break;
                        }
                    }

                    if (found == false)
                        return;
                }
            }
        }

        public override void OnBodyGUI()
        {
            Rect disolveRect;

            try
            {
                ObjectTree.BeginDraw(true);

                if (Value is IRejoin)
                {
                    foreach (var port in LooselyConnectedToThis)
                    {
                        if (port.Direction != IO.Output)
                            continue;

                        var end = Window.GetNodeEndpointPosition(port.CachedRect.center, this, port.Direction);

                        end.x = -4;
                        end.y -= Value.Position.y;

                        var timeRect = new Rect(end, new Vector2(16, 16));
                        timeRect.y -= timeRect.height / 2f;

                        GUI.Box(timeRect, _waitContent, SirenixGUIStyles.None);
                    }

                    GUIHelper.PushColor(new Color(1, 1, 1, 0.5f));
                    GUI.DrawTexture(new Rect(0, 6, 2, CachedSize.y-12), Texture2D.whiteTexture);
                    GUIHelper.PopColor();
                }

                var entriesDrawer = ObjectTree.GetPropertyAtPath(ValueEntries).Children;
                var gridspaceHeight = GUILayoutUtility.GetLastRect().y;
                gridspaceHeight += Value.Position.y + GetBodyStyle().border.top;

                gridspaceHeight += EditorGUIUtility.singleLineHeight;
                disolveRect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth, EditorGUIUtility.singleLineHeight);
                disolveRect.width = disolveRect.height;
                disolveRect.x = GetWidth() / 2f - disolveRect.width / 2f;

                while (_connectedToPortsRect.Count < entriesDrawer.Count)
                    _connectedToPortsRect.Add(new Rect(Value.Position, default));

                for (int i = 0; i < entriesDrawer.Count; i++)
                {
                    float yDiff = (_connectedToPortsRect[i].center.y - Port.Size / 2f) - gridspaceHeight;
                    if (yDiff < 0)
                        yDiff = 0;

                    GUILayoutUtility.GetRect(1, yDiff);
                    gridspaceHeight += yDiff;

                    entriesDrawer[i].Children[0].Draw(GUIContent.none);
                    gridspaceHeight += EditorGUIUtility.singleLineHeight;

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

                {
                    float highestYMax = gridspaceHeight;
                    foreach (var port in LooselyConnectedToThis)
                    {
                        if (port.NodeEditor is not BifurcateEditor)
                            highestYMax = MathF.Max(highestYMax, port.NodeEditor.CachedSize.y + port.NodeEditor.Value.Position.y);
                    }
                    foreach (var rect in _connectedToPortsRect)
                        highestYMax = MathF.Max(highestYMax, rect.yMax);

                    float diffToRect = highestYMax - gridspaceHeight;
                    if (diffToRect < 0)
                        diffToRect = 0;

                    GUILayoutUtility.GetRect(1, diffToRect);
                }

                gridspaceHeight += EditorGUIUtility.singleLineHeight;
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

            if (GUI.Button(disolveRect, _disolve, SirenixGUIStyles.None))
            {
                GUI.changed = true;
                var ports = (
                    from editor in Window.NodesToEditor
                    from port in editor.Value.ActivePorts
                    where port.Value.Connected == Value
                    orderby port.Value.CachedRect.y
                    select port.Value).ToArray();

                Undo.SetCurrentGroupName("Dissolve Bifurcate");
                int group = Undo.GetCurrentGroup();
                {
                    for (int i = 0; i < ports.Length; i++)
                    {
                        var port = ports[i];
                        if (Value.Entries.Length == 0)
                        {
                            port.Disconnect(true);
                        }
                        else
                        {
                            var dest = Value.Entries[Math.Min(i, Value.Entries.Length - 1)].Executable;
                            port.Connect(Window.NodesToEditor[dest], true);
                        }
                    }

                    Window.RemoveNode(this, true);
                }
                Undo.CollapseUndoOperations(group);
            }
        }

        private static readonly string ValueEntries = $"{nameof(NodeEditor.Value)}.{nameof(Bifurcate.Entries)}";
        private static GUIContent? __warnEntryNotConnected;
        private static GUIContent? __waitContent;
        private static Texture? __add, __remove, __disolve;
        private static GUIContent _warnEntryNotConnected => __warnEntryNotConnected ??= new GUIContent(EditorGUIUtility.IconContent("console.erroricon@2x").image, "Entry is not connected");
        private static GUIContent _waitContent => __waitContent ??= new GUIContent(EditorGUIUtility.IconContent("UnityEditor.AnimationWindow").image, "Any path reaching this node will block until all of them reached it");
        private static Texture _add => __add ??= EditorGUIUtility.IconContent("CollabCreate Icon").image;
        private static Texture _remove => __remove ?? EditorGUIUtility.IconContent("CollabDeleted Icon").image;
        private static Texture _disolve => __disolve ??= EditorGUIUtility.IconContent("d_Grid.EraserTool").image;
    }
}
