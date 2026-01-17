using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace Screenplay.Nodes.Triggers
{
    public class WhileSceneLoaded : Precondition
    {
        public required SceneReference Target;

        public override void CollectReferences(ReferenceCollector references) { }

        public override async UniTask Setup(IPreconditionCollector tracker, CancellationToken triggerCancellation)
        {
            if (SceneManager.GetSceneByPath(Target.Path).IsValid())
                tracker.SetUnlockedState(true);

            UnityEngine.Events.UnityAction<Scene> OnUnload = (scene) =>
            {
                if (scene.path == Target.Path)
                    tracker.SetUnlockedState(false);
            };
            UnityEngine.Events.UnityAction<Scene, LoadSceneMode> OnLoad = (scene, mode) =>
            {
                if (scene.path == Target.Path)
                    tracker.SetUnlockedState(true);
            };

            SceneManager.sceneUnloaded += OnUnload;
            SceneManager.sceneLoaded += OnLoad;

            await UniTask.WaitUntilCanceled(triggerCancellation);

            SceneManager.sceneUnloaded -= OnUnload;
            SceneManager.sceneLoaded -= OnLoad;
        }

    }
}
