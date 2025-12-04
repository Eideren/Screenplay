using System.Threading;
using Cysharp.Threading.Tasks;

namespace Screenplay
{
    public static class Extensions
    {
        public static async UniTask WithInterruptingCancellation(this UniTask task, CancellationToken cancellationToken)
        {
            // Let's see if this works
            #warning test this out
            await UniTask.WhenAny(task, UniTask.WaitUntilCanceled(cancellationToken, completeImmediately: true));
            cancellationToken.ThrowIfCancellationRequested();
        }

        public static async UniTask<T> WithInterruptingCancellation<T>(this UniTask<T> task, CancellationToken cancellationToken)
        {
            // Let's see if this works
#warning test this out
            var t = await UniTask.WhenAny(task, UniTask.WaitUntilCanceled(cancellationToken, completeImmediately: true));
            cancellationToken.ThrowIfCancellationRequested();
            return t.result;
        }
    }
}
