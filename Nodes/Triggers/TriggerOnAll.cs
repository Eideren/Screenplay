using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using YNode;

namespace Screenplay.Nodes.Triggers
{
    [Serializable]
    public class TriggerOnAll : AbstractScreenplayNode, IPrecondition
    {
        [SerializeReference, Input, ListDrawerSettings(AlwaysAddDefaultValue = true, ShowFoldout = false)]
        public IPrecondition?[] Sources = Array.Empty<IPrecondition?>();

        public override void CollectReferences(ReferenceCollector references) => references.Collect(Sources);

        public async UniTask Setup(IEventTracker tracker, CancellationToken triggerCancellation)
        {
            var door = new Door(tracker);
            foreach (var source in Sources)
                source?.Setup(new Door.Latch(door), triggerCancellation);

            while (triggerCancellation.IsCancellationRequested == false)
            {
                if (door.Closed)
                {
                    tracker.SetUnlockedState(false);
                    await UniTask.WhenAny(door.WaitOpen(), UniTask.WaitUntilCanceled(triggerCancellation, completeImmediately: true));
                }
                else
                {
                    tracker.SetUnlockedState(true, door.CollectPermutations());
                    await UniTask.WhenAny(door.WaitClosed(), UniTask.WaitUntilCanceled(triggerCancellation, completeImmediately: true));
                }
            }
        }
    }
}
