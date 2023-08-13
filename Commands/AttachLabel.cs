using SerializeReferenceUI;
using UnityEngine;

namespace Screenplay.Commands
{
    [CreateAssetMenu(menuName = "Screenplay/Attach Label")]
    public class AttachLabel : ScriptableObject, IInspectorString
    {
        public string GetInspectorString() => name;
        public Transform Transform;
    }
}