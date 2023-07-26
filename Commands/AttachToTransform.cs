using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Screenplay.Commands
{
    public class AttachToTransform : ICommand
    {
        public Transform Transform;
        public bool ClampToEdges;

        public void ValidateSelf()
        {
            if (Transform == null)
                throw new NullReferenceException(nameof(Transform));
        }

        public IEnumerable<(string name, IValidatable validatable)> GetSubValues() => this.NoSubValues();

        public string GetInspectorString() => $"Attach this line or choice to {(Transform == null ? "null" : Transform.name)}";

        public IEnumerable Run(Stage stage)
        {
            var rect = stage.ActiveFeed.rectTransform;
            if (rect.parent.GetComponent<Canvas>() == null)
                rect = (RectTransform)rect.parent;

            var attach = rect.gameObject.AddComponent<AttachedToTransform>();
            attach.Command = this;
            attach.RectTransform = rect;
            stage.OnDoneWithLine += _ => Object.Destroy(attach);
            yield break;
        }

        class AttachedToTransform : MonoBehaviour
        {
            public AttachToTransform Command;
            public RectTransform RectTransform;
            public void OnEnable() => Camera.onPreCull += OnCameraPreCull;
            public void OnDisable() => Camera.onPreCull -= OnCameraPreCull;

            void OnCameraPreCull(Camera cam)
            {
                Vector3 pos = cam.WorldToViewportPoint(Command.Transform.position);
                if (Command.ClampToEdges)
                {
                    for (int i = 0; i < 2; i++)
                        pos[i] = Mathf.Clamp01(pos[i]);
                }
                RectTransform.position = cam.ViewportToScreenPoint(pos);
            }
        }
    }
}