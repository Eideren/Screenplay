using System;
using System.Collections.Generic;
using Screenplay.Nodes.Barriers;
using UnityEngine;
using YNode;
using YNode.Editor;

namespace Screenplay.Nodes.Editor.Barriers
{
    public class IBarrierPartEditor : NodeEditor, ICustomNodeEditor<IBarrierPart>
    {
        private static GUIStyle? s_sharedStyle;

        public new IBarrierPart Value => (IBarrierPart)base.Value;

        public override void OnHeaderGUI() { }

        public override void OnBodyGUI()
        {
            IBarrierPart.InNodeEditor = true;

            base.OnBodyGUI();
            Value.NextBarrier?.UpdatePorts(Value);

            IBarrierPart.InNodeEditor = false;

            if (UnityEngine.Event.current.type == EventType.Repaint)
            {
                foreach (var port in Value.InheritedPorts)
                {
                    if (Window.NodesToEditor.ContainsKey(port) == false)
                    {
                        Graph.Nodes.Add(port);
                        Window.InitNodeEditorFor(port);
                    }
                }
            }
        }

        public override void PreRemoval()
        {
            base.PreRemoval();

            foreach (var port in Value.InheritedPorts)
            {
                if (Window.NodesToEditor.TryGetValue(port, out var editor))
                    Window.RemoveNode(editor);
            }
        }

        public override GUIStyle GetBodyStyle()
        {
            if (s_sharedStyle is null)
            {
                s_sharedStyle = new GUIStyle(GUIStyle.none)
                {
                    normal =
                    {
                        background = Texture2D.grayTexture
                    }
                };
            }

            return s_sharedStyle;
        }

        public override GUIStyle GetBodyHighlightStyle() => GUIStyle.none;

        public override Color GetTint() => Color.gray;

        [ThreadStatic] private static Queue<IBranch>? s_processQueue;

        protected static void AppendNodesAfterThisBarrier(IBarrierPart value, HashSet<INodeValue> nodes)
        {
            var processQueue = s_processQueue ??= new();
            for (IBarrierPart? barrier = value; barrier != null; barrier = barrier.NextBarrier)
            {
                if (nodes.Add(barrier) == false)
                    continue;

                foreach (var allTrack in barrier.AllTracks())
                {
                    if (allTrack.Branch is null)
                        continue;

                    if (nodes.Add(allTrack.Branch) == false)
                        continue;

                    processQueue.Enqueue(allTrack.Branch);
                }
            }

            while (processQueue.TryDequeue(out var enqueued))
            {
                foreach (var branch in enqueued.Followup())
                {
                    if (branch is null)
                        continue;

                    if (nodes.Add(branch) == false)
                        continue;

                    processQueue.Enqueue(branch);
                }
            }
        }
    }
}
