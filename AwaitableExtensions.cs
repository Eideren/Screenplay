using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Screenplay
{
    public static class UniTaskExtensions
    {
        public static async UniTask AwaitWithCancellation(this UniTask uniTask, CancellationToken token)
        {
            var completionSource = new UniTaskCompletionSource();
            var registered = token.Register(() => completionSource.TrySetCanceled());

            uniTask.GetAwaiter().OnCompleted(() =>
            {
                try
                {
                    uniTask.GetAwaiter().GetResult();
                    completionSource.TrySetResult();
                }
                catch(Exception e)
                {
                    completionSource.TrySetException(e);
                }
            });

            await completionSource.Task;
            await registered.DisposeAsync();
        }
    }
}
