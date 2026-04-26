using System;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using Screenplay;
using UnityEngine;

public class CancelableAutoResetEvent<T> where T : notnull
{
    private readonly Action<object> _onExternalCancel;
    private readonly Deque<(Cancellation token, Action continuation, int version)> _cancellations = new(4);
    private T? _currentVal;
    private int _version;
    private bool _closed, _recusion;

    public CancelableAutoResetEvent()
    {
        _onExternalCancel = OnExternalCancel;
    }

    private void CheckThread()
    {
        if (PlayerLoopHelper.IsMainThread == false)
            throw new InvalidOperationException("Cannot be used from multiple threads");
    }

    private void ThrowWhenClosed()
    {
        if (_closed)
            throw new OperationCanceledException($"{nameof(CancelableAutoResetEvent<T>)} closed");
    }

    /// <exception cref="OperationCanceledException">When closed or when the token is canceled</exception>
    public Awaitable NextSignal(Cancellation cancellation) => new(this, cancellation);

    private void RegisterNewContinuation(Action continuation, Cancellation cancellation)
    {
        CheckThread();
        ThrowWhenClosed();

        cancellation.ThrowIfCancellationRequested();
        _cancellations.AddToBack((cancellation, continuation, _version + 1));
        cancellation.Register(_onExternalCancel, continuation);
    }

    public void Signal(T value)
    {
        CheckThread();

        if (_closed)
            throw new InvalidOperationException($"{nameof(CancelableAutoResetEvent<T>)} has been closed, it cannot be signaled");

        if (_recusion)
            throw new InvalidOperationException($"{nameof(CancelableAutoResetEvent<T>)} called recursively, a task that was just waiting on this signal is signaling it");

        try
        {
            _recusion = true;

            _version++;
            _currentVal = value;
            while (_cancellations.Count > 0 && _cancellations.PeekBack().version == _version)
            {
                var (token, continuation, version) = _cancellations.RemoveFromBack();
                token.Unregister(_onExternalCancel, continuation);
                try
                {
                    continuation();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
        finally
        {
            _recusion = false;
            _currentVal = default;
        }
    }

    public void Close()
    {
        CheckThread();
        _closed = true;

        while (_cancellations.Count > 0)
        {
            var (token, continuation, version) = _cancellations.RemoveFromFront();
            token.Unregister(_onExternalCancel, continuation);
            try
            {
                continuation();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    private void OnExternalCancel(object o)
    {
        var continuation = (Action)o;
        for (int j = 0; j < _cancellations.Count; j++)
        {
            var d = _cancellations[j];
            if (d.continuation == continuation)
            {
                // We're not removing, as it makes signaling a bit more complex
                _cancellations[j] = (Cancellation.None, static () => { }, d.version);
                continuation.Invoke();
                break;
            }
        }
    }

    public readonly struct Awaitable
    {
        private readonly CancelableAutoResetEvent<T> _provider;
        private readonly Cancellation _ct;

        public Awaitable(CancelableAutoResetEvent<T> provider, Cancellation ct = default)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _ct = ct;
        }

        public Awaiter GetAwaiter() => new(_provider, _ct);

        public readonly struct Awaiter : ICriticalNotifyCompletion
        {
            private readonly CancelableAutoResetEvent<T> _provider;
            private readonly Cancellation _ct;
            private readonly int _version;

            public Awaiter(CancelableAutoResetEvent<T> provider, Cancellation ct)
            {
                _provider = provider;
                _ct = ct;
                _version = _provider._version + 1;
            }

            public bool IsCompleted => false;

            public void OnCompleted(Action continuation) => UnsafeOnCompleted(continuation);

            public void UnsafeOnCompleted(Action continuation) => _provider.RegisterNewContinuation(continuation, _ct);

            public T GetResult()
            {
                _ct.ThrowIfCancellationRequested();
                _provider.ThrowWhenClosed();

                if (_version != _provider._version)
                    throw new InvalidOperationException("This awaiter was queried more than one Signal ago somehow");

                return _provider._currentVal ?? throw new InvalidOperationException();
            }
        }
    }
}
