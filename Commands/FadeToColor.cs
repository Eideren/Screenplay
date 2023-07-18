using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.UI;
using Color = UnityEngine.Color;

namespace Screenplay.Commands
{
    [Serializable] public class FadeToColor : ICommand
    {
        static Dictionary<(byte, byte, byte), KnownColor> _knownColors = new();

        public float Duration = 1f;
        public Gradient Gradient;
        public UISortingOrder SortingOrder = UISortingOrder.AboveTextBox;

        public IEnumerable Run(Stage stage)
        {
            var fadeToColor = new GameObject(nameof(FadeToColor));
            var canvas = fadeToColor.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeToColor.AddComponent<CanvasRenderer>();
            var image = fadeToColor.AddComponent<RawImage>();
            canvas.sortingOrder = (int)SortingOrder;
            for (float f = 0f; f < 1f; f += Time.deltaTime / Duration)
            {
                image.color = Gradient.Evaluate(f);
                yield return null;
            }
            image.color = Gradient.Evaluate(1f);

            GameObject.Destroy(fadeToColor);
        }
        
        public void ValidateSelf(){}
        public IEnumerable<(string name, IValidatable validatable)> GetSubValues() => this.NoSubValues();
        public string GetInspectorString() => $"Fade to {ShortColorString(Gradient.Evaluate(1f))} over {Duration} seconds";

        static string ShortColorString(Color c)
        {
            if (_knownColors.Count == 0)
            {
                lock (_knownColors)
                {
                    if (_knownColors.Count == 0)
                    {
                        _knownColors.Clear();
                        foreach (KnownColor kc in Enum.GetValues(typeof(KnownColor)))
                        {
                            if (kc <= KnownColor.WindowText || kc >= KnownColor.ButtonFace)
                                continue;

                            System.Drawing.Color c2 = System.Drawing.Color.FromKnownColor(kc);
                            _knownColors[(c2.R, c2.G, c2.B)] = kc;
                        }
                    }
                }
            }

            (byte, byte, byte) key = ((byte)(c.r*255f), (byte)(c.g*255f), (byte)(c.b*255f));
            if (_knownColors.TryGetValue(key, out KnownColor knownColor))
            {
                if (c.a == 1f)
                    return knownColor.ToString();
                return $"{knownColor} with {c.a:F1} alpha";
            }

            return c.r == 0f && c.g == 0f && c.b == 0f ? $"(rgb:0,a:{c.a})" : $"({c.r:F1}, {c.g:F1}, {c.b:F1}, {c.a:F1})";
        }
    }
}