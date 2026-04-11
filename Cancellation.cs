using System;
using System.Threading;

namespace Screenplay
{
    public readonly struct Cancellation
    {
        private readonly CancellationSource? _source;

        public static Cancellation None => new();

        public Cancellation(CancellationSource source) => _source = source;

        public bool IsCancellationRequested => _source?.IsCancellationRequested ?? false;
        public bool CanBeCanceled => _source is not null;

        public void ThrowIfCancellationRequested()
        {
            if (IsCancellationRequested)
                throw new OperationCanceledException();
        }

        public CancellationToken GetStandardToken() => _source?.GetStandardToken() ?? CancellationToken.None;

        public void Register(Action action) => _source?.Register(action);
        public void Register(Action<object> action, object parameter) => _source?.Register(action, parameter);
        public void Unregister(Action action) => _source?.Unregister(action);
        public void Unregister(Action<object> action, object parameter) => _source?.Unregister(action, parameter);

        public void Register(CancellationSource source) => _source?.Register(source);
    }
}
