using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Screenplay
{
    public class CancelableCompletionSource<T>
    {
        private readonly Action<object> _onExternalCancel;
        private readonly List<Cancellation> _cancellations = new(4);
        private readonly List<Action> _continuations = new(4);
        private T _currentVal = default!;
        private State _state;

        private enum State
        {
            Idle,
            Successful,
            Canceled
        }

        public CancelableCompletionSource()
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

        public Awaitable AwaitResult(Cancellation cancellation) => new(this, cancellation);

        private void RegisterNewContinuation(Action continuation, Cancellation cancellation)
        {
            CheckThread();

            cancellation.ThrowIfCancellationRequested();
            switch (_state)
            {
                case State.Idle:
                    _continuations.Add(continuation);
                    _cancellations.Add(cancellation);
                    cancellation.Register(_onExternalCancel, continuation);
                    break;
                case State.Successful:
                    try
                    {
                        continuation();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                    break;
                case State.Canceled:
                    throw new TaskCanceledException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void SetResult(T value)
        {
            CheckThread();

            if (_state != State.Idle)
                throw new InvalidOperationException();
            _currentVal = value;
            _state = State.Successful;

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

            _continuations.Clear();
            _cancellations.Clear();
        }

        public void SetCanceled()
        {
            CheckThread();

            if (_state != State.Idle)
                throw new InvalidOperationException();

            _state = State.Canceled;

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

            _continuations.Clear();
            _cancellations.Clear();
        }


        public readonly struct Awaitable
        {
            readonly CancelableCompletionSource<T> provider;
            readonly Cancellation ct;

            public Awaitable(CancelableCompletionSource<T> provider, Cancellation ct = default)
            {
                this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
                this.ct = ct;
            }

            public Awaiter GetAwaiter() => new(provider, ct);

            public readonly struct Awaiter : ICriticalNotifyCompletion
            {
                private readonly CancelableCompletionSource<T> _provider;
                private readonly Cancellation _ct;

                public Awaiter(CancelableCompletionSource<T> provider, Cancellation ct)
                {
                    _provider = provider;
                    _ct = ct;
                }

                public bool IsCompleted => false;

                public void OnCompleted(Action continuation) => UnsafeOnCompleted(continuation);

                public void UnsafeOnCompleted(Action continuation) => _provider.RegisterNewContinuation(continuation, _ct);

                public T GetResult()
                {
                    _ct.ThrowIfCancellationRequested();
                    switch (_provider._state)
                    {
                        case State.Idle:
                            throw new InvalidOperationException();
                        case State.Successful:
                            return _provider._currentVal;
                        case State.Canceled:
                            throw new TaskCanceledException();
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }
    }
}
