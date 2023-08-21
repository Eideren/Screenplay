using SerializeReferenceUI;
using UnityEngine;

namespace Screenplay.Commands
{
    [CreateAssetMenu(menuName = "Screenplay/Attach Label")]
    public class AttachLabel : ScriptableObject, IInspectorString
    {
        [HideInInspector] public Transform Transform;
        public string GetInspectorString() => name;
    }
}