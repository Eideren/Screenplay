using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Screenplay
{
    public class CustomizedPicker : EditorWindow
    {
        private Logic _objectPicker = null!;

        public static void Show<T>(IEnumerable<T?> source, Rect field, Action<T?> onPick, string title = $"Select {nameof(T)}")
        {
            var window = CreateInstance<CustomizedPicker>();
            window._objectPicker = new PickerLogic<T>
            {
                Items = source.ToList(),
                OnPick = onPick,
                Window = window
            };
            window.titleContent = new GUIContent(title);

            var screenRect = field;
            screenRect.position = GUIUtility.GUIToScreenPoint(screenRect.position);

            window.ShowAsDropDown(screenRect, new Vector2(screenRect.width, 400));
        }

        private void OnGUI()
        {
            _objectPicker.OnGUI();
        }

        private abstract class Logic
        {
            public abstract void OnGUI();
        }

        private class PickerLogic<T> : Logic
        {
            public required Action<T?> OnPick;
            public required List<T?> Items;
            public required EditorWindow Window;
            private string _search = "";
            private Vector2 _scroll;

            public override void OnGUI()
            {
                GUILayout.BeginHorizontal(EditorStyles.toolbar);
                _search = GUILayout.TextField(_search, GUI.skin.FindStyle("ToolbarSearchTextField"));
                GUILayout.EndHorizontal();

                _scroll = EditorGUILayout.BeginScrollView(_scroll);

                foreach (var item in Filtered())
                {
                    Rect rect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));

                    GUIContent content = EditorGUIUtility.ObjectContent(item as Object, typeof(T));

                    if (GUI.Button(rect, content, EditorStyles.toolbarButton))
                    {
                        OnPick.Invoke(item);
                        Window.Close();
                    }
                }

                EditorGUILayout.EndScrollView();
            }

            private IEnumerable<T?> Filtered()
            {
                if (string.IsNullOrEmpty(_search))
                    return Items;

                return Items.Where(i => i != null && i is Object o && o.name.Contains(_search, StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}
