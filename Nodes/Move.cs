using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using YNode;

namespace Screenplay.Nodes
{
    [NodeVisuals(Icon = "EchoFilter Icon")]
    public class Move : ExecutableLinear, INodeWithSceneGizmos
    {
        [HideLabel] public SceneObjectReference<GameObject> Target;
        [HideLabel] public Vector3 Destination;
        [HideLabel] public Quaternion Rotation = Quaternion.identity;

        public override void CollectReferences(ReferenceCollector references) => references.Collect(Target);

        protected override UniTask LinearExecution(IEventContext context, CancellationToken cancellation)
        {
            if (Target.TryGet(out var go, out var failure) == false)
            {
                Debug.LogWarning($"Failed to {nameof(Move)}, {nameof(Target)}: {failure}", context.Source);
                return UniTask.CompletedTask;
            }

            go.transform.position = Destination;
            go.transform.rotation = Rotation;
            return UniTask.CompletedTask;
        }

        public override void FastForward(IEventContext context, CancellationToken cancellationToken)
        {
            if (Target.TryGet(out var go, out var failure) == false)
            {
                Debug.LogWarning($"Failed to {nameof(Move)}, {nameof(Target)}: {failure}", context.Source);
                return;
            }

            go.transform.position = Destination;
            go.transform.rotation = Rotation;
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

            go.transform.position = Destination;
            go.transform.rotation = Rotation;
        }

        public void DrawGizmos(ref bool rebuildPreview)
        {
            #if UNITY_EDITOR
            var newPosition = UnityEditor.Handles.PositionHandle(Destination, Rotation);
            var newRotation = UnityEditor.Handles.RotationHandle(Rotation, Destination);
            rebuildPreview |= newPosition != Destination || newRotation != Rotation;
            Destination = newPosition;
            Rotation = newRotation;
            #endif
        }
    }
}
