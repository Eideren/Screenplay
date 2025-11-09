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
    public class MultiTrigger : AbstractScreenplayNode, ITriggerSetup
    {
        [SerializeReference, Input, ListDrawerSettings(AlwaysAddDefaultValue = true, ShowFoldout = false)]
        public ITriggerSetup?[] Sources = Array.Empty<ITriggerSetup?>();

        public override void CollectReferences(List<GenericSceneObjectReference> references)
        {
            foreach (var source in Sources)
                source?.CollectReferences(references);
        }

        public async UniTask<IAnnotation?> AwaitTrigger(CancellationToken cancellation)
        {
            var list = new UniTask<IAnnotation?>[Sources.Length];
            for (int i = 0; i < Sources.Length; i++)
            {
                ITriggerSetup? setup = Sources[i];
                if (setup is null)
                    continue;

                list[i] = setup.AwaitTrigger(cancellation);
            }

            return (await UniTask.WhenAny(list)).result;
        }
    }
}
