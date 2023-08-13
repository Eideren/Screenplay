using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Screenplay.Commands
{
    public class AttachToLabel : ICommand
    {
        public AttachLabel Label;
        public bool KeepInsideTheScreen;
        [Tooltip("Do not log an error when no Attach Points are set in the scene for this label")]
        public bool FailSilently;

        public void ValidateSelf()
        {
            if (Label == null)
                throw new NullReferenceException(nameof(Transform));
        }

        public IEnumerable<(string name, IValidatable validatable)> GetSubValues() => this.NoSubValues();

        public string GetInspectorString() => $"Attach this line or choice to {(Label == null ? "null" : Label.name)}";

        public IEnumerable Run(Stage stage)
        {
            if (Label.Transform is null)
            {
                if (FailSilently == false)
                    Debug.LogError($"No {nameof(AttachPoint)} in scene bound to {Label}", Label);
                yield break;
            }

            var rect = stage.ActiveFeed.rectTransform;
            if (rect.parent.GetComponent<Canvas>() == null)
                rect = (RectTransform)rect.parent;

            var attach = rect.gameObject.AddComponent<AttachedToTransform>();
            attach.Command = this;
            attach.RectTransform = rect;
            attach.InitialPosition = rect.position;
            stage.OnDoneWithLine += _ => Object.Destroy(attach);
            yield break;
        }

        class AttachedToTransform : MonoBehaviour
        {
            public AttachToLabel Command;
            public RectTransform RectTransform;
            public Vector3 InitialPosition;

            public void OnEnable() => Camera.onPreCull += OnCameraPreCull;

            public void OnDisable()
            {
                Camera.onPreCull -= OnCameraPreCull;
                RectTransform.position = InitialPosition;
            }

            void OnCameraPreCull(Camera cam)
            {
                if (Command.Label.Transform is null)
                {
                    enabled = false;
                    return;
                }

                Vector3 pos = cam.WorldToScreenPoint(Command.Label.Transform.position);
                if (Command.KeepInsideTheScreen)
                {
                    Vector2 screenSize = cam.pixelRect.size;
                    Vector2 size = RectTransform.rect.size;
                    Vector2 pivot = RectTransform.pivot;
                    Vector2 min = pivot * size;
                    Vector2 max = screenSize - (Vector2.one - pivot) * size;
                    for (int i = 0; i < 2; i++)
                        pos[i] = Mathf.Clamp(pos[i], min[i], max[i]);
                }
                RectTransform.position = pos;
            }
        }
    }
}