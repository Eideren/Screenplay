using System;
using System.Threading;
using Screenplay.Nodes;
using UnityEngine;

namespace Screenplay
{
    public interface IPreviewer : IContext
    {
        bool Loop { get; }
        void RegisterRollback(System.Action rollback);
        void RegisterRollback(AnimationClip clip, GameObject go);
        void PlayCustomSignal(Func<CancellationToken, Awaitable> signal);

        void PlayCustomSignal<T>(Func<CancellationToken, Awaitable<T>> signal)
        {
            PlayCustomSignal(cts => AwaitableOfTWrapper(cts, signal));

            static async Awaitable AwaitableOfTWrapper(CancellationToken cancellation, Func<CancellationToken, Awaitable<T>> signal) => await signal(cancellation);
        }

        void PlaySafeAction(IExecutable executable)
        {
            PlayCustomSignal(PreviewPlay);

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
