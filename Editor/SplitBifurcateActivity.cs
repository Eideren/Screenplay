using System;
using System.Collections.Generic;
using Screenplay.Nodes;
using UnityEditor;
using UnityEngine;
using YNode.Editor;
using Event = UnityEngine.Event;

namespace Screenplay.Editor
{
    public class SplitBifurcateActivity : NodeActivity
    {
        private static Texture? __rejoinIcon, __bifurcateIcon;
        private static Texture _rejoinIcon => __rejoinIcon ??= EditorGUIUtility.IconContent("UnityEditor.Graphs.AnimatorControllerTool@2x").image;
        private static Texture _bifurcateIcon => __bifurcateIcon ??= EditorGUIUtility.IconContent("Git@2x").image;
        private Vector2 _startingPos;
        private List<Port> _intersectsWith = new();
        private Vector2 _nodeCreationPosition;

        public SplitBifurcateActivity(GraphWindow window, Vector2 startingPos) : base(window)
        {
            _startingPos = startingPos;
        }

        public override void InputPreDraw(Event e)
        {
            switch (e.type)
            {
                case EventType.MouseUp when e.button == 0:
                    Window.CurrentActivity = null;

                    if (_intersectsWith.Count > 0)
                    {
                        Undo.SetCurrentGroupName("Split Bifurcate");
                        int group = Undo.GetCurrentGroup();
                        var rejoinEditor = Window.CreateNode(_intersectsWith.Count > 1 ? typeof(Rejoin) : typeof(Bifurcate), Window.WindowToGridPosition(_nodeCreationPosition), true);
                        var rejoin = (Bifurcate)rejoinEditor.Value;
                        rejoin.Entries = new Bifurcate.ExecutableEntry[_intersectsWith.Count];

                        for (int i = 0; i < _intersectsWith.Count; i++)
                        {
                            var port = _intersectsWith[i];

                            if (port.Connected is IExecutable exec)
                                rejoin.Entries[i].Executable = exec;

                            port.Connect(rejoinEditor, true);
                        }
                        Undo.CollapseUndoOperations(group);
                    }

                    GUI.changed = true;
                    e.Use();
                    Window.Repaint();
                    break;

                case EventType.MouseDrag:
                    Window.Repaint();
                    e.Use();
                    break;
            }
        }

        private static List<Vector2> _cacheA = new();
        private static List<Vector3> _cacheB = new();

        public override void PreNodeDraw()
        {
            var textureToUse = _intersectsWith.Count > 1 ? _rejoinIcon : _bifurcateIcon;
            _intersectsWith.Clear();
            var pA = _startingPos;
            var pB = Event.current.mousePosition;
            GraphWindow.DrawAAPolyLineWithShadowNonAlloc(4, _startingPos, pB);

            _nodeCreationPosition.x = (pA.x + pB.x) / 2f;
            _nodeCreationPosition.y = MathF.Min(pA.y, pB.y);

            foreach (var (value, editor) in Window.NodesToEditor)
            {
                foreach (var (_, port) in editor.ActivePorts)
                {
                    if (port.Direction == IO.Input)
                        continue;

                    _cacheA.Clear();
                    if (Window.GetPathFor(port, _cacheA, out var windowRect, out _, out _) == false
                        || Window.ShouldWindowRectBeCulled(windowRect))
                    {
                        continue;
                    }

                    _cacheB.Clear();
                    Window.NoodleBuild(Window.GetNoodlePath(port, port.ConnectedEditor), _cacheA, _cacheB);
                    for (int i = 0; i < _cacheB.Count - 1; i++)
                    {
                        if (!SegmentIntersection(pA, pB, _cacheB[i], _cacheB[i + 1], out Vector2 intersection))
                            continue;

                        _intersectsWith.Add(port);
                        var iconSize = new Vector2(16, 16);
                        GUI.DrawTexture(new Rect(intersection - iconSize / 2, iconSize), textureToUse);
                        break;
                    }
                }
            }

            return;

            static bool SegmentIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersection)
            {
                Vector2 r = p2 - p1;
                Vector2 s = p4 - p3;

                float rxs = Cross(r, s);
                float qpxr = Cross(p3 - p1, r);

                // Collinear
                if (Math.Abs(rxs) < float.Epsilon && Math.Abs(qpxr) < float.Epsilon)
                {
                    float t0 = Dot(p3 - p1, r) / Dot(r, r);
                    float t1 = t0 + Dot(s, r) / Dot(r, r);

                    if (r.sqrMagnitude < float.Epsilon) // r is a point
                    {
                        if ((p1 - p3).sqrMagnitude < float.Epsilon)
                        {
                            intersection = p1;
                            return true;
                        }

                        intersection = default;
                        return false;
                    }

                    // Ensure t0 <= t1
                    if (t1 < t0)
                    {
                        (t1, t0) = (t0, t1);
                    }

                    // Overlap if intervals [t0,t1] and [0,1] intersect
                    if (t0 <= 1f && t1 >= 0f)
                    {
                        // Choose a point in the overlap; here clamp t0 to [0,1]
                        float t = MathF.Max(0f, t0);
                        intersection = p1 + t * r;
                        return true;
                    }

                    intersection = default;
                    return false;
                }

                if (Math.Abs(rxs) < float.Epsilon && Math.Abs(qpxr) >= float.Epsilon)
                {
                    intersection = default;
                    return false;
                }

                float t2 = Cross(p3 - p1, s) / rxs;
                float u = Cross(p3 - p1, r) / rxs;

                if (rxs != 0 && t2 is >= 0f and <= 1f && u is >= 0f and <= 1f)
                {
                    intersection = p1 + t2 * r;
                    return true;
                }

                intersection = default;
                return false;
            }

            static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
            static float Dot(Vector2 a, Vector2 b) => a.x * b.x + a.y * b.y;
        }

        public override void PostNodeDraw()
        {

        }

        public override void InputPostDraw(Event e)
        {
        }
    }
}
