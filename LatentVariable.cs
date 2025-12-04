using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Screenplay
{
    public class LatentVariable<T> : IDisposable where T : IEquatable<T>
    {
        public T Value { get; private set; }
        private readonly AutoResetUniTaskCompletionSource _changed = AutoResetUniTaskCompletionSource.Create();

        public LatentVariable(T initialValue) => Value = initialValue;

        public void Dispose() => _changed.TrySetCanceled();

        public async UniTask Await(T expect, CancellationToken cancellationToken)
        {
            while (Value.Equals(expect) == false)
            {
                await _changed.Task.WithInterruptingCancellation(cancellationToken);
            }
        }

        public async UniTask AwaitNot(T expect, CancellationToken cancellationToken)
        {
            while (Value.Equals(expect))
            {
                await _changed.Task.WithInterruptingCancellation(cancellationToken);
            }
        }

        public void Set(T value)
        {
            if (Value.Equals(value))
                return;

            Value = value;
            _changed.TrySetResult();
        }
    }
}
