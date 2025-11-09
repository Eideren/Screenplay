using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Screenplay.Nodes.Triggers
{
    [Serializable]
    public class TriggerZone : AbstractScreenplayNode, ITriggerSetup
    {
        [Required, ValidateInput(nameof(IsTrigger))] public SceneObjectReference<Collider> Target;
        public LayerMask LayerMask = ~0;

        private bool IsTrigger(SceneObjectReference<Collider> target, ref string message)
        {
            if (target.TryGet(out var obj, out _) && obj.isTrigger == false)
            {
                message = $"{obj.name} must have {nameof(obj.isTrigger)} enabled";
                return false;
            }

            return true;
        }

        public override void CollectReferences(List<GenericSceneObjectReference> references) => references.Add(Target);

        public async UniTask<IAnnotation?> AwaitTrigger(CancellationToken cancellation)
        {
            Collider? obj;
            while (Target.TryGet(out obj, out _) == false)
                await UniTask.NextFrame(cancellation, true);

            var output = obj.gameObject.AddComponent<TriggerZoneComponent>();
            output.LayerMask = LayerMask;
            try
            {
                await output.Completion.Task;
                return null;
            }
            finally
            {
                Object.Destroy(output);
            }
        }

        private class TriggerZoneComponent : MonoBehaviour
        {
            public readonly UniTaskCompletionSource Completion = new();
            public LayerMask LayerMask;

            private void OnTriggerStay(Collider collider)
            {
                GameObject go;
                if (collider is CharacterController cc)
                    go = cc.gameObject;
                else
                    go = collider.attachedRigidbody.gameObject;

                if ((LayerMask & 1 << go.layer) != 0)
                    Trigger();
            }

            private void OnDisable() => Completion.TrySetCanceled();

            [Button("Force Trigger")]
            public void Trigger() => Completion.TrySetResult();
        }
    }
}
