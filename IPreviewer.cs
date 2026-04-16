using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Screenplay
{
    public interface IPreviewer : IEventContext
    {
        List<IScreenplayNode> Path { get; }
        bool Loop { get; }
        void RegisterRollback(System.Action rollback);
        void RegisterTRSRollback(Transform trs);
        void RegisterRollback(AnimationClip clip, GameObject go);
        void RegisterBoneOnlyRollback(Animator animator);
        void RegisterRollback(Animator animator, int hash, int layer);
        void RegisterRollback(Animator animator, AnimationClip clip);
        void AddCustomPreview(Func<Cancellation, UniTask> signal);

        void PlaySafeAction(IExecutable executable)
        {
            AddCustomPreview(PreviewPlay);

            async UniTask PreviewPlay(Cancellation cancellation)
            {
                await executable.Execute(this, cancellation);

                if (Loop)
                {
                    await Uni.Delay(1f, cancellation: cancellation, cancelImmediately:true);
                }
            }
        }
    }
}
