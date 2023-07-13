using System;
using UnityEngine;

namespace Screenplay
{
    public struct GUIBackgroundColorScope : IDisposable
    {
        readonly Color previousColor;

        public GUIBackgroundColorScope(Color color)
        {
            previousColor = GUI.backgroundColor;
            GUI.backgroundColor = color;
        }

        public void Dispose()
        {
            GUI.backgroundColor = previousColor;
        }
    }
}