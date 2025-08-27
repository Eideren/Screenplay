using System.Collections.Generic;
using System.Threading;
using Sirenix.OdinInspector;
using UnityEngine;
// Async method lacks 'await' operators and will run synchronously - done on purpose
#pragma warning disable CS1998

namespace Screenplay.Nodes
{
    public class Move : ExecutableLinear, INodeWithSceneGizmos
    {
        [HideLabel] public SceneObjectReference<GameObject> Target;
        [HideLabel] public Vector3 Destination;
        [HideLabel] public Quaternion Rotation = Quaternion.identity;

        public override void CollectReferences(List<GenericSceneObjectReference> references) => references.Add(Target);

        protected override async Awaitable LinearExecution(IEventContext context, CancellationToken cancellation)
        {
            if (Target.TryGet(out var go, out var failure) == false)
            {
                Debug.LogWarning($"Failed to {nameof(Move)}, {nameof(Target)}: {failure}", context.Source);
                return;
            }

            go.transform.position = Destination;
            go.transform.rotation = Rotation;
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
