using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SerializeReferenceUI
{
    public static class SerializeReferenceInspectorMiddleMouseMenu
    {
        public static void ShowContextMenuForManagedReferenceOnMouseMiddleButton(this SerializedProperty property, Rect position, IEnumerable<Func<Type, bool>> filters = null)
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDown && position.Contains(e.mousePosition) && e.button == 2)
                property.ShowContextMenuForManagedReference(null, false, filters);
        }
    }
}