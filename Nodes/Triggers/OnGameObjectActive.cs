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
    public class OnGameObjectActive : AbstractScreenplayNode, ITriggerSetup
    {
        [Required] public SceneObjectReference<GameObject> Target;

        public override void CollectReferences(List<GenericSceneObjectReference> references) { references.Add(Target); }

        public async UniTask<IAnnotation?> AwaitTrigger(CancellationToken cancellation)
        {
            GameObject? obj;
            while (Target.TryGet(out obj, out _) == false)
                await UniTask.NextFrame(cancellation, cancelImmediately:true);

            var output = obj.AddComponent<OnGameObjectActiveComp>();
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

        private class OnGameObjectActiveComp : MonoBehaviour
        {
            public readonly UniTaskCompletionSource Completion = new();

            private void OnEnable() => Completion.TrySetResult();

            private void OnDestroy() => Completion.TrySetCanceled();
        }
    }
}
