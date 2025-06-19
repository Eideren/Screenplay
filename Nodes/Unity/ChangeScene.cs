using System.Collections.Generic;
using System.Threading;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Screenplay.Nodes.Unity
{
    public class ChangeScene : Action
    {
        [Required, HideLabel, HorizontalGroup] public SceneReference Scene;

        public override void CollectReferences(List<GenericSceneObjectReference> references) { }

        public override async Awaitable<IAction?> Execute(IContext context, CancellationToken cancellation)
        {
            var a = SceneManager.LoadSceneAsync(Scene.Path, LoadSceneMode.Single);
            a!.allowSceneActivation = true;
            await a;
            return Next;
        }

        public override void FastForward(IContext context)
        {
            SceneManager.LoadScene(Scene.Path, LoadSceneMode.Single);
        }

        public override void SetupPreview(IPreviewer previewer, bool fastForwarded)
        {

        }
    }
}
