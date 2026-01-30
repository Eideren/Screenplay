using System.Threading;
using Cysharp.Threading.Tasks;

public static class UniTaskExtensions
{
    public static async UniTask WithInterruptingCancellation(this UniTask task, CancellationToken cancellationToken)
    {
        await task.AttachExternalCancellation(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
    }

    public static async UniTask<T> WithInterruptingCancellation<T>(this UniTask<T> task, CancellationToken cancellationToken)
    {
        var val = await task.AttachExternalCancellation(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return val;
    }
}
