using UnityEngine;

namespace Screenplay.Commands
{
    public class AttachPoint : MonoBehaviour
    {
        public AttachLabel Label;

        void OnValidate()
        {
            if (Label == null)
                Debug.LogError($"No {nameof(Label)} set for this {nameof(AttachPoint)}", this);
        }

        void OnEnable() => Label.Transform = transform;

        void OnDisable()
        {
            if (Label.Transform == transform)
                Label.Transform = null;
        }
    }
}