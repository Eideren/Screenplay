using System;
using UnityEditor;
using UnityEngine;

namespace Screenplay.Editor
{
    public struct ScrollView : IDisposable
    {
        public ScrollView(ref Vector2 scrollPosition, params GUILayoutOption[] options)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, options);
        }

        public void Dispose()
        {
            EditorGUILayout.EndScrollView();
        }
    }

    public class Horizontal : IDisposable
    {
        public Horizontal() => EditorGUILayout.BeginHorizontal();

        public void Dispose() => EditorGUILayout.EndHorizontal();
    }

    public class Vertical : IDisposable
    {
        public Vertical() => EditorGUILayout.BeginVertical();

        public void Dispose() => EditorGUILayout.EndVertical();
    }
}