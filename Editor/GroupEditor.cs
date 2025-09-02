using System;
using UnityEngine;
using YNode.Editor;
using Screenplay.Nodes;
using UnityEditor;
using Event = UnityEngine.Event;

namespace Screenplay.Editor
{
    public class GroupEditor : NodeEditor, ICustomNodeEditor<Group>
    {
        public new Group Value => (Group)base.Value;

        private const int EdgeWidth = 14;

        private static GUIStyle BodyStyle
        {
            get
            {
                if (s_body != null)
                    return s_body;

                s_body = new GUIStyle
                {
                    normal =
                    {
                        background = NodeBody
                    },
                    border = new RectOffset(32, 32, 32, 32),
                    padding = new RectOffset(16, 16, 4, 16)
                };
                return s_body;
            }
        }

        private bool _childrenManipulation;
        private Vector2? _lastPos;

        public override void OnHeaderGUI()
        {
            DrawEditableTitle(ref Value.Description);
        }

        public override void OnBodyGUI()
        {
            var previousBG = GUI.backgroundColor;
            var previousContent = GUI.color;
            var backgroundTint = GetTint();
            GUI.backgroundColor = backgroundTint;
            GUI.color = backgroundTint;

            Span<Rect> areas = stackalloc Rect[]
            {
                new Rect(0,0,EdgeWidth,Value.Size.y),
                new Rect(0,0,Value.Size.x,EdgeWidth),
                new Rect(Value.Size.x - EdgeWidth,0,EdgeWidth,Value.Size.y),
                new Rect(0,Value.Size.y - EdgeWidth,Value.Size.x,EdgeWidth),
            };

            Span<(MouseCursor, Rect)> edges = stackalloc (MouseCursor, Rect)[]
            {
                (MouseCursor.ResizeUpLeft, new Rect(0,0,EdgeWidth,EdgeWidth)),
                (MouseCursor.ResizeUpRight, new Rect(Value.Size.x - EdgeWidth,0,EdgeWidth,EdgeWidth)),
                (MouseCursor.ResizeUpLeft, new Rect(Value.Size.x - EdgeWidth,Value.Size.y - EdgeWidth,EdgeWidth,EdgeWidth)),
                (MouseCursor.ResizeUpRight, new Rect(0,Value.Size.y - EdgeWidth,EdgeWidth,EdgeWidth)),
            };

            var e = Event.current;
            var axes = ScaleAxes.None;
            for (int i = 0; i < areas.Length; i++)
            {
                if (areas[i].Contains(e.mousePosition)
                    && Window.CurrentActivity is null
                    && e is { button: 0, type: EventType.MouseDown })
                {
                    axes |= (ScaleAxes)(1 << i);
                }
            }

            for (int i = 0; i < edges.Length; i++)
            {
                if (Window.CurrentActivity is null && e.type == EventType.Repaint)
                    AddCursorRectFromBody(edges[i].Item2, edges[i].Item1);
            }

            for (int i = 0; i < areas.Length; i++)
            {
                if (Window.CurrentActivity is null && e.type == EventType.Repaint)
                    AddCursorRectFromBody(areas[i], i % 2 == 0 ? MouseCursor.ResizeHorizontal : MouseCursor.ResizeVertical);
            }

            if (axes != 0)
            {
                e.Use();
                Window.CurrentActivity = new ResizeGroupActivity(this, axes, Window);
            }

            GUI.backgroundColor = new Color();
            GUI.color = backgroundTint.grayscale > 0.5f ? Color.black : Color.white;
            BodyStyle.active.textColor = backgroundTint.grayscale > 0.5f ? Color.black : Color.white;

            var sizeLeft = new Vector2(Value.Size.x, Value.Size.y - TitleHeight - GetBodyStyle().padding.bottom);
            GUILayoutUtility.GetRect(sizeLeft.x, sizeLeft.x, sizeLeft.y, sizeLeft.y);

            GUI.backgroundColor = previousBG;
            GUI.color = previousContent;

            _childrenManipulation = false;
            if (Window.CurrentActivity is DragNodeActivity dragActivity)
            {
                var groupRect = new Rect(Value.Position, CachedSize);
                foreach (var editor in dragActivity.Editors)
                {
                    if (editor == this)
                        continue;

                    var draggedRect = new Rect(editor.Value.Position, editor.CachedSize);
                    var minRect = draggedRect;
                    minRect.min = Vector2.Max(groupRect.min, draggedRect.min);
                    minRect.max = Vector2.Min(groupRect.max, draggedRect.max);
                    bool isOverGroup = draggedRect == minRect;
                    bool isInGroup = Value.Children.Contains(editor.Value);
                    if (isOverGroup == isInGroup)
                        continue;

                    _childrenManipulation = true;
                    bool willRemove = isInGroup;
                    if (e.rawType == EventType.MouseUp && e.button == 0)
                    {
                        if (willRemove)
                            Value.Children.Remove(editor.Value);
                        else
                            Value.Children.Add(editor.Value);
                    }
                }
            }

            if (_lastPos != Value.Position)
            {
                if (_lastPos is { } lastPos)
                {
                    var delta = Value.Position - lastPos;
                    foreach (var nodeValue in Value.Children)
                    {
                        nodeValue.Position += delta;
                    }
                }

                _lastPos = Value.Position;
            }
        }

        public override int GetWidth()
        {
            return Mathf.CeilToInt(Value.Size.x);
        }

        public override GUIStyle GetBodyStyle()
        {
            return BodyStyle;
        }

        public override Color GetTint()
        {
            return _childrenManipulation ? Color.white : Color.gray;
        }

        public override bool HitTest(Rect rect, Vector2 mousePosition)
        {
            const int headerSize = 30;

            rect = Window.GridToWindowRect(rect);
            mousePosition = Window.GridToWindowPosition(mousePosition);
            var delta = rect.center - mousePosition;
            var s = rect.size / 2f;
            if (MathF.Abs(delta.x) >= s.x - EdgeWidth)
                return true;
            if (MathF.Abs(delta.y) >= s.y - EdgeWidth)
                return true;

            return mousePosition.y < rect.y + headerSize;
        }

        [Flags]
        public enum ScaleAxes
        {
            None = 0,
            Left = 0b0001,
            Up = 0b0010,
            Right = 0b0100,
            Down = 0b1000,
            UpLeft = Left | Up,
            DownLeft = Left | Down,
            UpRight = Right | Up,
            DownRight = Right | Down,
        }

        public class ResizeGroupActivity : NodeActivity
        {
            public readonly GroupEditor Group;
            public readonly ScaleAxes Axes;
            public Vector4 UnroundedPos;

            public ResizeGroupActivity(GroupEditor group, ScaleAxes axes, GraphWindow window) : base(window)
            {
                Group = group;
                Axes = axes;
                UnroundedPos = new Vector4(Group.Value.Position.x, Group.Value.Position.y, Group.Value.Size.x, Group.Value.Size.y);
            }

            public override void InputPreDraw(Event e)
            {
                var type = e.type;
                switch (type)
                {
                    case EventType.MouseDrag when e.button == 0:
                    {
                        bool gridSnap = Preferences.GetSettings().GridSnap;
                        if (e.control)
                            gridSnap = !gridSnap;

                        for (int i = 0; i < 4; i++)
                        {
                            if ((Axes & (ScaleAxes)(1 << i)) != 0)
                            {
                                UnroundedPos[i] += e.delta[i % 2] * Window.Zoom;
                                if (i < 2)
                                    UnroundedPos[i + 2] -= e.delta[i % 2] * Window.Zoom;
                                e.Use();
                            }
                        }

                        Group.Value.Position = new Vector2(UnroundedPos.x, UnroundedPos.y);
                        Group.Value.Size = new Vector2(UnroundedPos.z, UnroundedPos.w);
                        if (gridSnap)
                        {
                            Group.Value.Position = new(
                                (Mathf.Round((Group.Value.Position.x + 8) / 16) * 16) - 8,
                                (Mathf.Round((Group.Value.Position.y + 8) / 16) * 16) - 8);
                            Group.Value.Size = new(
                                (Mathf.Round((Group.Value.Size.x + 8) / 16) * 16) - 8,
                                (Mathf.Round((Group.Value.Size.y + 8) / 16) * 16) - 8);
                        }
                        break;
                    }
                    case EventType.MouseUp when e.button == 0:
                        Window.CurrentActivity = null;
                        e.Use();
                        break;
                }
            }

            public override void PreNodeDraw()
            {

            }

            public override void PostNodeDraw()
            {

            }

            public override void InputPostDraw(Event e)
            {
                var r = new Rect(-20, -20, 40, 40);
                r.position += e.mousePosition;
                if (Axes is ScaleAxes.UpRight or ScaleAxes.DownLeft)
                {
                    EditorGUIUtility.AddCursorRect(r, MouseCursor.ResizeUpRight, 0);
                }
                else if (Axes is ScaleAxes.UpLeft or ScaleAxes.DownRight)
                {
                    EditorGUIUtility.AddCursorRect(r, MouseCursor.ResizeUpLeft, 0);
                }
                else
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if ((Axes & (ScaleAxes)(1 << i)) != 0)
                        {
                            EditorGUIUtility.AddCursorRect(r, i % 2 == 0 ? MouseCursor.ResizeHorizontal : MouseCursor.ResizeVertical, 0);
                        }
                    }
                }
            }
        }

        private static GUIStyle? s_body;
        private static Texture2D? s_nodeBody;
        private static Texture2D NodeBody =>
            s_nodeBody != null ? s_nodeBody : s_nodeBody = UnityEngine.Resources.Load<Texture2D>("ynode_group");
    }
}
