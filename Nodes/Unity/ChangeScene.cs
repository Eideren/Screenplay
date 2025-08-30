using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Screenplay.Nodes.Unity
{
    public class ChangeScene : ExecutableLinear
    {
        [Required, HideLabel, HorizontalGroup] public SceneReference Scene;

        public override void CollectReferences(List<GenericSceneObjectReference> references) { }

        protected override async UniTask LinearExecution(IEventContext context, CancellationToken cancellation)
        {
            var a = SceneManager.LoadSceneAsync(Scene.Path, LoadSceneMode.Single);
            a!.allowSceneActivation = true;
            await a;
        }

        public override void FastForward(IEventContext context, CancellationToken cancellationToken)
        {
            SceneManager.LoadScene(Scene.Path, LoadSceneMode.Single);
        }

        public override void SetupPreview(IPreviewer previewer, bool fastForwarded)
        {

        }
    }
}
