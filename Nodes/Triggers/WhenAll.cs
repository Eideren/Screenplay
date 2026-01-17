using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using YNode;

namespace Screenplay.Nodes.Triggers
{
    [Serializable, NodeVisuals(Icon = "UnityEditor.Graphs.AnimatorControllerTool@2x")]
    public class WhenAll : Precondition
    {
        [SerializeReference, Input, ListDrawerSettings(AlwaysAddDefaultValue = true, ShowFoldout = false)]
        public Precondition[] Sources = Array.Empty<Precondition>();

        public override void CollectReferences(ReferenceCollector references) => references.Collect(Sources);

        public override async UniTask Setup(IPreconditionCollector tracker, CancellationToken triggerCancellation)
        {
            var door = new Door(tracker, Sources, out IPreconditionCollector[] collectors);

            for (int i = 0; i < Sources.Length; i++)
                Sources[i].Setup(collectors[i], triggerCancellation).Forget();

            while (triggerCancellation.IsCancellationRequested == false)
            {
                if (door.Open)
                {
                    tracker.SetUnlockedState(true);
                    await door.WaitClosed().WithInterruptingCancellation(triggerCancellation);
                }
                else
                {
                    tracker.SetUnlockedState(false);
                    await door.WaitOpen().WithInterruptingCancellation(triggerCancellation);
                }
            }
        }
    }
}
