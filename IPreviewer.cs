using System;
using System.Collections.Generic;
using System.Threading;
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
        void RegisterRollback(Animator animator, int hash, int layer);
        void AddCustomPreview(Func<CancellationToken, Awaitable> signal);

        void PlayCustomSignal<T>(Func<CancellationToken, Awaitable<T>> signal)
        {
            AddCustomPreview(cts => AwaitableOfTWrapper(cts, signal));

            static async Awaitable AwaitableOfTWrapper(CancellationToken cancellation, Func<CancellationToken, Awaitable<T>> signal) => await signal(cancellation);
        }

        void PlaySafeAction(IExe<IEventContext> executable)
        {
            AddCustomPreview(PreviewPlay);

            async Awaitable PreviewPlay(CancellationToken cancellation)
            {
                do
                {
                    await executable.InnerExecution(this, cancellation);

                    if (Loop)
                    {
                        await Awaitable.WaitForSecondsAsync(1f, cancellation);
                    }
                } while (Loop);
            }
        }
    }
}
