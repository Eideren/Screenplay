using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using Sirenix.OdinInspector;
using UnityEngine;
using YNode;

namespace Screenplay.Nodes
{
    [NodeVisuals(Icon = "EchoFilter Icon")]
    public class Move : ExecutableLinear, INodeWithSceneGizmos
    {
        public float Duration = 1f;
        public EasingType EasingType = EasingType.InOut;
        [HideLabel] public SceneObjectReference<GameObject> Target;
        [HideLabel] public Vector3 Destination;
        [HideLabel] public Quaternion Rotation = Quaternion.identity;

        public override void CollectReferences(ReferenceCollector references) => references.Collect(Target);

        protected override async UniTask LinearExecution(IEventContext context, Cancellation cancellation)
        {
            if (Target.TryGet(out var go, out var failure) == false)
            {
                Debug.LogWarning($"Failed to {nameof(Move)}, {nameof(Target)}: {failure}", context.Source);
                return;
            }

            var startPos = go.transform.position;
            var startRot = go.transform.rotation;
            float f = 0;
            do
            {
                await UniTaskExtensions.NextFrame(cancellation, cancelImmediately: true);
                f = Mathf.Clamp01(f + Time.deltaTime);
                go.transform.position = Vector3.Lerp(startPos, Destination, f);
                go.transform.rotation = Quaternion.Lerp(startRot, Rotation, f);
            } while (f < Duration);
        }

        public override async UniTask Persistence(IEventContext context, Cancellation cancellation)
        {
            do
            {
                var go = await Target.GetAsync(cancellation);
                go.transform.position = Destination;
                go.transform.rotation = Rotation;
                await go.OnDestroyAsync();
            } while (cancellation.IsCancellationRequested == false);
        }

        public override void SetupPreview(IPreviewer previewer, bool fastForwarded)
        {
            if (Target.TryGet(out var go, out var failure) == false)
                return;

            var previousPosition = go.transform.position;
            var previousRotation = go.transform.rotation;
            previewer.RegisterRollback(() =>
            {
                go.transform.position = previousPosition;
                go.transform.rotation = previousRotation;
            });
            if (fastForwarded)
            {
                go.transform.position = Destination;
                go.transform.rotation = Rotation;
            }
            else
            {
                previewer.AddCustomPreview(Preview);

                async UniTask Preview(Cancellation cts)
                {
                    await LinearExecution(previewer, cts);
                }
            }
        }

        public void DrawGizmos(SceneGUIProxy guiProxy, ScreenplayGraph graph, ref bool rebuildPreview)
        {
            Destination = guiProxy.PositionHandle(Destination, Rotation);
            Rotation = guiProxy.RotationHandle(Rotation, Destination);
        }
    }
}
