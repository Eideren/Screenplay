using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using UnityEngine;
using YNode;

namespace Screenplay.Nodes
{
    [NodeVisuals(Icon = "d_AnimationClip Icon")]
    public class PlayAnimation : ExecutableLinear
    {
        public required SceneObjectReference<GameObject> Target;
        public required AnimationClip? Clip = null;

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
                await UniTask.NextFrame(cancellation, cancelImmediately:true);
            } while (t < Clip.length);
        }

        public override async UniTask Persistence(IEventContext context, CancellationToken cancellationToken)
        {
            do
            {
                var go = await Target.GetAsync(cancellationToken);
                if (Clip == null)
                {
                    Debug.LogWarning($"Failed to {nameof(PlayAnimation)} on '{go}', {nameof(Clip)} is null", context.Source);
                }
                else
                {
                    using var sampler = new AnimationSampler(Clip, go);
                    sampler.SampleAt(Clip.length);
                }
                await go.OnDestroyAsync();
            } while (cancellationToken.IsCancellationRequested == false);
        }

        public override void SetupPreview(IPreviewer previewer, bool fastForwarded)
        {
            if (Target.TryGet(out var go, out _) == false || Clip == null)
                return;

            previewer.RegisterRollback(Clip, go);
            if (fastForwarded)
            {
                using var sampler = new AnimationSampler(Clip, go);
                sampler.SampleAt(Clip.length);
            }
            else
            {
                previewer.PlaySafeAction(this);
            }
        }

        public override void CollectReferences(ReferenceCollector references) => references.Collect(Target);
    }
}
