using System;
using System.Collections.Generic;
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
                var node = (BarrierEnd)Window.CreateNode(typeof(BarrierEnd), Value.Position + Vector2.right * (100 + CachedSize.x)).Value;
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

            EditorGUI.DrawRect(Window.GridToWindowRect(_rect), new Color(0, 0, 0, 0.25f));

            for (IBarrierPart? part = Value; part != null; part = part.NextBarrier)
            {
                if (part.NextBarrier is null)
                    break;

                part.NextBarrier.Position = new(part.NextBarrier.Position.x, part.Position.y);
                if (part.NextBarrier.Position.x < part.Position.x + Barrier.Width)
                    part.NextBarrier.Position = new Vector2(part.Position.x + Barrier.Width, part.Position.y);

                var midpoint = (part.Position + new Vector2(Barrier.Width, 0) + part.NextBarrier.Position) * 0.5f;
                var rect = Window.GridToWindowRect(new Rect(midpoint - Vector2.right * ButtonSize.x * 0.5f, ButtonSize));
                if (GUI.Button(rect, EditorIcons.Plus.ActiveGUIContent))
                {
                    InsertNewBarrierAfter(Window, part);
                }
            }
        }

        public override void OnBodyGUI()
        {
            base.OnBodyGUI();

            if (GUILayout.Button(EditorIcons.Plus.ActiveGUIContent))
            {
                GUI.changed = true;
                Array.Resize(ref Value.Tracks, Value.Tracks.Length+1);
                Value.Tracks[^1] = new EventOutput();
                Value.UpdatePorts();
            }
        }

        public override void PreRemoval()
        {
            base.PreRemoval();

            var list = new List<IBarrierPart>();
            for (IBarrierPart? part = Value; part != null; part = part.NextBarrier)
                list.Add(part);

            for (int i = list.Count - 1; i >= 0; i--)
                Window.RemoveNode(Window.NodesToEditor[list[i]]);
        }

        private static void InsertNewBarrierAfter(GraphWindow window, IBarrierPart value)
        {
            var cachedSize = window.NodesToEditor[value].CachedSize;
            var newBarrier = (BarrierIntermediate)window.CreateNode(typeof(BarrierIntermediate), value.Position + Vector2.right * (100 + cachedSize.x)).Value;
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
    }
}
