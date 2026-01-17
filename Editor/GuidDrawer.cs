using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Screenplay.Editor
{
    public sealed class GuidDrawer : OdinValueDrawer<guid>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            EditorGUILayout.LabelField(Property.NiceName, Property.ValueEntry.WeakSmartValue.ToString());
        }
    }
}
