using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using UnityEngine;

namespace Screenplay.Nodes.Triggers
{
    [Serializable]
    public class OnGameObjectActive : AbstractScreenplayNode, IPrecondition
    {
        public required SceneObjectReference<GameObject> Target;

        public override void CollectReferences(List<GenericSceneObjectReference> references) { references.Add(Target); }

        public async UniTask Setup(IEventTracker tracker, CancellationToken triggerCancellation)
        {
            while (triggerCancellation.IsCancellationRequested == false)
            {
                var target = await Target.GetAsync(triggerCancellation);

                var output = target.gameObject;
                await output.GetAsyncEnableTrigger().OnEnableAsync(triggerCancellation);
                tracker.SetUnlockedState(true);
                await output.GetAsyncDisableTrigger().OnDisableAsync(triggerCancellation);
                tracker.SetUnlockedState(false);
            }
        }
    }
}
