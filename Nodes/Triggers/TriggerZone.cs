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
    public class TriggerZone : AbstractScreenplayNode, IPrecondition
    {
        [ValidateInput(nameof(IsTrigger))] public required SceneObjectReference<Collider> Target;
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

        public override void CollectReferences(ReferenceCollector references) => references.Collect(Target);

        public async UniTask Setup(IEventTracker tracker, CancellationToken triggerCancellation)
        {
            while (triggerCancellation.IsCancellationRequested == false)
            {
                var target = await Target.GetAsync(triggerCancellation);

                var trigger = target.gameObject.AddComponent<TriggerZoneComponent>();
                trigger.Tracker = tracker;
                trigger.LayerMask = LayerMask;

                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(trigger.destroyCancellationToken, triggerCancellation))
                {
                    await UniTask.WaitUntilCanceled(cts.Token, completeImmediately: true);
                }

                Object.Destroy(trigger);

                triggerCancellation.ThrowIfCancellationRequested();
            }
        }

        private class TriggerZoneComponent : MonoBehaviour
        {
            public LayerMask LayerMask;
            public int Count;
            public required IEventTracker Tracker;

            private void OnTriggerEnter(Collider other)
            {
                GameObject go;
                if (other is CharacterController cc)
                    go = cc.gameObject;
                else
                    go = other.attachedRigidbody.gameObject;

                if ((LayerMask & 1 << go.layer) != 0)
                {
                    Count++;
                    Tracker.SetUnlockedState(true);
                }
            }

            private void OnTriggerExit(Collider other)
            {
                GameObject go;
                if (other is CharacterController cc)
                    go = cc.gameObject;
                else
                    go = other.attachedRigidbody.gameObject;

                if ((LayerMask & 1 << go.layer) != 0)
                {
                    Count--;
                    Tracker.SetUnlockedState(Count != 0);
                }
            }

            private void OnDisable() => Tracker.SetUnlockedState(false);

            [Button("Force Trigger")]
            public void Trigger() => Tracker.SetUnlockedState(true);
        }
    }
}
