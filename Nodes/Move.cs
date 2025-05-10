using System.Collections.Generic;
using System.Threading;
using Sirenix.OdinInspector;
using UnityEngine;
// Async method lacks 'await' operators and will run synchronously - done on purpose
#pragma warning disable CS1998

namespace Screenplay.Nodes
{
    public class Move : Action, INodeWithSceneGizmos
    {
        [HideLabel] public SceneObjectReference<GameObject> Target;
        [HideLabel] public Vector3 Destination;
        [HideLabel] public Quaternion Rotation = Quaternion.identity;

        public override void CollectReferences(List<GenericSceneObjectReference> references) => references.Add(Target);

        public override async Awaitable<IAction?> Execute(IContext context, CancellationToken cancellation)
        {
            if (Target.TryGet(out var go, out var failure) == false)
            {
                Debug.LogWarning($"Failed to {nameof(Move)}, {nameof(Target)}: {failure}", context.Source);
                return Next;
            }

            go.transform.position = Destination;
            go.transform.rotation = Rotation;
            return Next;
        }

        public override void FastForward(IContext context)
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
