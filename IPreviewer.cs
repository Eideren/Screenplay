using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Screenplay.Nodes;
using UnityEngine;

namespace Screenplay
{
    public interface IPreviewer : IEventContext
    {
        List<IScreenplayNode> Path { get; }
        bool Loop { get; }
        void RegisterRollback(System.Action rollback);
        void RegisterRollback(AnimationClip clip, GameObject go);
        void RegisterBoneOnlyRollback(Animator animator);
        void RegisterRollback(Animator animator, int hash, int layer);
        void RegisterRollback(Animator animator, AnimationClip clip);
        void AddCustomPreview(Func<CancellationToken, UniTask> signal);

        void PlayCustomSignal<T>(Func<CancellationToken, UniTask<T>> signal)
        {
            AddCustomPreview(cts => UniTaskOfTWrapper(cts, signal));

            static async UniTask UniTaskOfTWrapper(CancellationToken cancellation, Func<CancellationToken, UniTask<T>> signal) => await signal(cancellation);
        }

        void PlaySafeAction(IExe<IEventContext> executable)
        {
            AddCustomPreview(PreviewPlay);

            async UniTask PreviewPlay(CancellationToken cancellation)
            {
                await executable.InnerExecution(this, cancellation);

                if (Loop)
                {
                    await UniTask.WaitForSeconds(1f, cancellationToken: cancellation, cancelImmediately:true);
                }
            }
        }
    }
}
