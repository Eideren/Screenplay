using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Screenplay;
using UnityEngine;

public class CancelableAutoResetEvent<T> where T : notnull
{
    private readonly Action<object> _onExternalCancel;
    private readonly List<Cancellation> _cancellations = new(4);
    private readonly List<Action> _continuations = new(4);
    private T? _currentVal;
    private int _version;
    private bool _closed, _internal;

    public CancelableAutoResetEvent()
    {
        _onExternalCancel = o =>
        {
            var continuation = (Action)o;
            int i = _continuations.IndexOf(continuation);
            // We're not removing, as it makes signaling a bit more complex
            _continuations[i] = static () => { };
            _cancellations[i] = Cancellation.None;
            continuation.Invoke();
        };
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

    public Awaitable NextSignal(Cancellation cancellation) => new(this, cancellation);

    private void RegisterNewContinuation(Action continuation, Cancellation cancellation)
    {
        CheckThread();
        ThrowWhenClosed();

        cancellation.ThrowIfCancellationRequested();
        _continuations.Add(continuation);
        _cancellations.Add(cancellation);
        cancellation.Register(_onExternalCancel, continuation);
    }

    public void Signal(T value)
    {
        CheckThread();

        try
        {
            if (_closed)
                throw new InvalidOperationException();

            if (_internal)
                throw new InvalidOperationException();
            _internal = true;

            _version++;
            _currentVal = value;
            int end = _continuations.Count;
            for (int i = 0; i < end; i++)
            {
                var continuation = _continuations[i];
                _cancellations[i].Unregister(_onExternalCancel, continuation);
                _cancellations[i] = default;
                _continuations[i] = static () => { };
                try
                {
                    continuation();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            _continuations.RemoveRange(0, end);
            _cancellations.RemoveRange(0, end);
        }
        finally
        {
            _internal = false;
            _currentVal = default;
        }
    }

    public void Close()
    {
        CheckThread();
        _closed = true;

        for (int i = 0; i < _continuations.Count; i++)
        {
            var continuation = _continuations[i];
            _cancellations[i].Unregister(_onExternalCancel, continuation);
            _cancellations[i] = default;
            _continuations[i] = static () => { };
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


    public readonly struct Awaitable
    {
        readonly CancelableAutoResetEvent<T> provider;
        readonly Cancellation ct;

        public Awaitable(CancelableAutoResetEvent<T> provider, Cancellation ct = default)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
            this.ct = ct;
        }

        public Awaiter GetAwaiter() => new(provider, ct);

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
