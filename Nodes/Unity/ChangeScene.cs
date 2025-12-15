using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using YNode;

namespace Screenplay.Nodes.Unity
{
    [NodeVisuals(Icon = "d_TerrainInspector.TerrainToolAdd")]
    public class ChangeScene : ExecutableLinear
    {
        [HideLabel, HorizontalGroup] public required SceneReference Scene;

        public override void CollectReferences(ReferenceCollector references) { }

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
