using System;
using System.Threading;
using UnityEngine;

namespace Screenplay
{
    public static class AwaitableExtensions
    {
        public static async Awaitable AwaitWithCancellation(this Awaitable awaitable, CancellationToken token)
        {
            var completionSource = new AwaitableCompletionSource();
            var registered = token.Register(() => completionSource.TrySetCanceled());

            awaitable.GetAwaiter().OnCompleted(() =>
            {
                try
                {
                    awaitable.GetAwaiter().GetResult();
                    completionSource.TrySetResult();
                }
                catch(Exception e)
                {
                    completionSource.TrySetException(e);
                }
            });

            await completionSource.Awaitable;
            await registered.DisposeAsync();
        }

        public static async Awaitable<T> AwaitWithCancellation<T>(this Awaitable<T> awaitable, CancellationToken token)
        {
            var completionSource = new AwaitableCompletionSource<T>();
            var registered = token.Register(() => completionSource.TrySetCanceled());

            awaitable.GetAwaiter().OnCompleted(() =>
            {
                try
                {
                    var r = awaitable.GetAwaiter().GetResult();
                    completionSource.TrySetResult(r);
                }
                catch(Exception e)
                {
                    completionSource.TrySetException(e);
                }
            });

            var v = await completionSource.Awaitable;
            await registered.DisposeAsync();
            return v;
        }
    }
}
