using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using YNode;

namespace Screenplay.Nodes.Triggers
{
    [Serializable]
    public class WhenAny : Precondition
    {
        [SerializeReference, Input, ListDrawerSettings(AlwaysAddDefaultValue = true, ShowFoldout = false)]
        public Precondition[] Sources = Array.Empty<Precondition>();

        public override void CollectReferences(ReferenceCollector references) => references.Collect(Sources);

        public override async UniTask Setup(IPreconditionCollector tracker, CancellationToken triggerCancellation)
        {
            var @lock = new Lock(tracker, Sources, out var collectors);
            for (int i = 0; i < Sources.Length; i++)
                Sources[i].Setup(collectors[i], triggerCancellation).Forget();

            while (triggerCancellation.IsCancellationRequested == false)
            {
                if (@lock.Open)
                {
                    tracker.SetUnlockedState(true);
                    await @lock.WaitClosed().WithInterruptingCancellation(triggerCancellation);
                }
                else
                {
                    tracker.SetUnlockedState(false);
                    await @lock.WaitOpen().WithInterruptingCancellation(triggerCancellation);
                }
            }
        }
    }
}
