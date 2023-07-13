using System;
using UnityEngine;

namespace Screenplay
{
    public struct GUIColorScope : IDisposable
    {
        readonly Color previousColor;

        public GUIColorScope(Color color)
        {
            previousColor = GUI.color;
            GUI.color = color;
        }

        public void Dispose()
        {
            GUI.color = previousColor;
        }
    }
}