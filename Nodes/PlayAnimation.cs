using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Screenplay.Nodes
{
    public class PlayAnimation : ExecutableLinear
    {
        [Required] public SceneObjectReference<GameObject> Target;
        [Required] public AnimationClip? Clip = null;

        protected override async UniTask LinearExecution(IEventContext context, CancellationToken cancellation)
        {
            if (Target.TryGet(out var go, out var failure) == false)
            {
                Debug.LogWarning($"Failed to {nameof(PlayAnimation)}, {nameof(Target)}: {failure}", context.Source);
                return;
            }
            if (Clip == null)
            {
                Debug.LogWarning($"Failed to {nameof(PlayAnimation)} on '{go}', {nameof(Clip)} is null", context.Source);
                return;
            }

            using var sampler = new AnimationSampler(Clip, go);
            float t = 0f;
            do
            {
                t += Time.deltaTime;
                t = t > Clip.length ? Clip.length : t;
                sampler.SampleAt(t);
                await UniTask.NextFrame(cancellation);
            } while (t < Clip.length);
        }

        public override void FastForward(IEventContext context, CancellationToken cancellationToken)
        {
            if (Target.TryGet(out var go, out var failure) == false)
            {
                Debug.LogWarning($"Failed to {nameof(PlayAnimation)}, {nameof(Target)}: {failure}", context.Source);
                return;
            }
            if (Clip == null)
            {
                Debug.LogWarning($"Failed to {nameof(PlayAnimation)} on '{go}', {nameof(Clip)} is null", context.Source);
                return;
            }

            using var sampler = new AnimationSampler(Clip, go);
            sampler.SampleAt(Clip.length);
        }

        public override void SetupPreview(IPreviewer previewer, bool fastForwarded)
        {
            if (Target.TryGet(out var go, out _) == false || Clip == null)
                return;

            previewer.RegisterRollback(Clip, go);
            if (fastForwarded)
                FastForward(previewer, CancellationToken.None);
            else
                previewer.PlaySafeAction(this);
        }

        public override void CollectReferences(List<GenericSceneObjectReference> references)
        {
            references.Add(Target);
        }
    }
}
