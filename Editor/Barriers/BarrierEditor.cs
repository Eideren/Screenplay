using System.Collections.Generic;
using System.Linq;
using Screenplay.Nodes.Barriers;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using YNode;
using YNode.Editor;

namespace Screenplay.Nodes.Editor.Barriers
{
    public class BarrierEditor : IBarrierPartEditor, ICustomNodeEditor<Barrier>
    {
        private static readonly Vector2 ButtonSize = new Vector2(30, 30);

        public new Barrier Value => (Barrier)base.Value;
        private Rect _rect;

        public void DrawBackground()
        {
            if (Value.NextBarrier == null!)
            {
                var node = (BarrierEnd)Window.CreateNode(typeof(BarrierEnd), Value.Position + Vector2.right * (100 + CachedSize.x), false).Value;
                Value.NextBarrier = node;
                Value.NextBarrier.UpdatePorts(Value);
            }

            if (UnityEngine.Event.current.type == EventType.Layout)
            {
                _rect = new Rect(Value.Position, default);
                var nodesToMove = new HashSet<INodeValue>();
                AppendNodesAfterThisBarrier(Value, nodesToMove);
                foreach (var nodeValue in nodesToMove)
                {
                    var thisNodesRect = new Rect(nodeValue.Position, Window.NodesToEditor[nodeValue].CachedSize);
                    _rect.min = Vector2.Min(thisNodesRect.min, _rect.min);
                    _rect.max = Vector2.Max(thisNodesRect.max, _rect.max);
                }
            }

            var backgroundRect = Window.GridToWindowRect(_rect);
            var grabbableBackgroundRect = backgroundRect;
            grabbableBackgroundRect.height = 20;
            grabbableBackgroundRect.y -= grabbableBackgroundRect.height;
            bool cursorHover = grabbableBackgroundRect.Contains(UnityEngine.Event.current.mousePosition) && Window.CurrentActivity is null
                                                                                                         && Window.HoveredNode is null
                                                                                                         && Window.HoveredPort is null;
            if (cursorHover || Window.CurrentActivity is DragBarriersActivity dbg && dbg.Barrier == Value)
                EditorGUI.DrawRect(grabbableBackgroundRect, new Color(0, 0, 0, 0.25f));

            GUI.Box(grabbableBackgroundRect, EditorIcons.HamburgerMenu.ActiveGUIContent);
            if (UnityEngine.Event.current.type == EventType.MouseDown
                && UnityEngine.Event.current.button == 0
                && cursorHover)
            {
                Window.CurrentActivity = new DragBarriersActivity(Window, UnityEngine.Event.current.mousePosition, Value);
            }

            EditorGUIUtility.AddCursorRect(grabbableBackgroundRect, MouseCursor.Pan);

            EditorGUI.DrawRect(backgroundRect, new Color(0, 0, 0, 0.25f));

            for (IBarrierPart? part = Value; part != null; part = part.NextBarrier)
            {
                if (part.NextBarrier is null)
                    break;

                part.NextBarrier.Position = new(part.NextBarrier.Position.x, part.Position.y);
                if (part.NextBarrier.Position.x < part.Position.x + Barrier.Width)
                    part.NextBarrier.Position = new Vector2(part.Position.x + Barrier.Width, part.Position.y);

                var midpoint = (part.Position + new Vector2(Barrier.Width, 0) + part.NextBarrier.Position) * 0.5f;
                var rect = Window.GridToWindowRect(new Rect(midpoint - Vector2.right * (ButtonSize.x * 0.5f), ButtonSize));
                if (GUI.Button(rect, EditorIcons.Plus.ActiveGUIContent))
                {
                    InsertNewBarrierAfter(Window, part);
                }
            }
        }

        public override void PreRemoval()
        {
            base.PreRemoval();

            var list = new List<IBarrierPart>();
            for (IBarrierPart? part = Value; part != null; part = part.NextBarrier)
                list.Add(part);

            for (int i = list.Count - 1; i >= 0; i--)
                Window.RemoveNode(Window.NodesToEditor[list[i]], true);
        }

        private static void InsertNewBarrierAfter(GraphWindow window, IBarrierPart value)
        {
            var cachedSize = window.NodesToEditor[value].CachedSize;
            var newBarrier = (BarrierIntermediate)window.CreateNode(typeof(BarrierIntermediate), value.Position + Vector2.right * (100 + cachedSize.x), true).Value;
            if (value.NextBarrier is { } previousBarrier)
            {
                var delta = previousBarrier.Position - value.Position;
                newBarrier.Position = value.Position;
                newBarrier.NextBarrier = value.NextBarrier;
                value.NextBarrier = newBarrier;

                var nodesToMove = new HashSet<INodeValue>();
                AppendNodesAfterThisBarrier(newBarrier, nodesToMove);
                foreach (var nodeValue in nodesToMove)
                    nodeValue.Position += delta;
            }

            value.NextBarrier = newBarrier;
            value.NextBarrier.UpdatePorts(value);
        }

        public class DragBarriersActivity : NodeActivity
        {
            public readonly Vector2[] DragOffset;
            public readonly NodeEditor[] Editors;
            public readonly Barrier Barrier;

            public DragBarriersActivity(GraphWindow window, Vector2 mousePosition, Barrier barrier) : base(window)
            {
                Barrier = barrier;
                var p = window.WindowToGridPosition(mousePosition);

                var set = new HashSet<INodeValue>();
                AppendNodesAfterThisBarrier(barrier, set);
                Editors = set.Select(x => window.NodesToEditor[x]).ToArray();
                DragOffset = new Vector2[Editors.Length + window.SelectedReroutes.Count];

                for (int i = 0; i < Editors.Length; i++)
                    DragOffset[i] = Editors[i].Value.Position - p;

                for (int i = 0; i < window.SelectedReroutes.Count; i++)
                    DragOffset[Editors.Length + i] = window.SelectedReroutes[i].GetPoint() - p;
            }

            public override void InputPreDraw(UnityEngine.Event e)
            {
                EditorGUIUtility.AddCursorRect(new Rect(default, Window.position.size), MouseCursor.Pan);
                switch (e.type)
                {
                    case EventType.MouseDrag when e.button == 0:

                        // Holding ctrl inverts grid snap
                        bool gridSnap = Preferences.GetSettings().GridSnap;
                        if (e.control)
                            gridSnap = !gridSnap;

                        Vector2 mousePos = Window.WindowToGridPosition(e.mousePosition);
                        // Move selected nodes with offset
                        for (int i = 0; i < Editors.Length; i++)
                        {
                            NodeEditor node = Editors[i];
                            Undo.RecordObject(node, "Moved Node");
                            Vector2 initial = node.Value.Position;
                            node.Value.Position = mousePos + DragOffset[i];
                            if (gridSnap)
                            {
                                node.Value.Position = new(
                                    (Mathf.Round((node.Value.Position.x + 8) / 16) * 16) - 8,
                                    (Mathf.Round((node.Value.Position.y + 8) / 16) * 16) - 8);
                            }

                            // Offset portConnectionPoints instantly if a node is dragged so they aren't delayed by a frame.
                            Vector2 offset = node.Value.Position - initial;
                            if (offset.sqrMagnitude > 0)
                            {
                                foreach (var (_, port) in node.Ports)
                                {
                                    Rect rect = port.CachedRect;
                                    rect.position += offset;
                                    port.CachedRect = rect;
                                }
                            }
                        }

                        // Move selected reroutes with offset
                        for (int i = 0; i < Window.SelectedReroutes.Count; i++)
                        {
                            Vector2 pos = mousePos + DragOffset[Editors.Length + i];
                            if (gridSnap)
                            {
                                pos.x = Mathf.Round(pos.x / 16) * 16;
                                pos.y = Mathf.Round(pos.y / 16) * 16;
                            }

                            Window.SelectedReroutes[i].SetPoint(pos);
                        }

                        Window.Repaint();
                        e.Use();
                        GUI.changed = true;
                        break;
                }
            }

            public override void PreNodeDraw()
            {
            }

            public override void PostNodeDraw()
            {
            }

            public override void InputPostDraw(UnityEngine.Event e)
            {
                switch (e.type)
                {
                    case EventType.MouseUp when e.button == 0:
                        Window.CurrentActivity = null;
                        e.Use();
                        break;
                }
            }
        }
    }
}
