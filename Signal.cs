using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class Signal<T> : IDisposable
{
    const int InitialSize = 16;

    private readonly object runningAndQueueLock = new object();
    private readonly object arrayLock = new object();

    private bool disposed = false;
    private int tail = 0;
    private bool running = false;
    private Promise?[] taskScheduled = new Promise[InitialSize];
    private Queue<Promise> waitQueue = new(InitialSize);

    public UniTask<T> NewTask(CancellationToken cancellationToken = default, bool cancelImmediately = false)
    {
        if (disposed)
            throw new ObjectDisposedException("A disposed signal cannot be awaited");

        return new UniTask<T>(Promise.Create(this, cancellationToken, cancelImmediately, out var token), token);
    }

    private void AddAction(Promise item)
    {
        lock (runningAndQueueLock)
        {
            if (running)
            {
                waitQueue.Enqueue(item);
                return;
            }
        }

        lock (arrayLock)
        {
            // Ensure Capacity
            if (taskScheduled.Length == tail)
            {
                Array.Resize(ref taskScheduled, checked(tail * 2));
            }
            taskScheduled[tail++] = item;
        }
    }

    public void Dispose()
    {
        lock (arrayLock)
        {
            disposed = true;

            CancelWaitingSignals();
        }
    }

    public void CancelWaitingSignals()
    {
        lock (arrayLock)
        {
            for (var index = 0; index < taskScheduled.Length; index++)
            {
                if (taskScheduled[index] == null)
                    continue;

                try
                {
                    taskScheduled[index]!.core.TrySetCanceled();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                taskScheduled[index] = null;
            }

            tail = 0;
        }
    }

    public void Send(T value)
    {
        lock (runningAndQueueLock)
        {
            running = true;
        }

        lock (arrayLock)
        {
            var j = tail - 1;

            for (int i = 0; i < taskScheduled.Length; i++)
            {
                var action = taskScheduled[i];
                if (action != null)
                {
                    try
                    {
                        if (!action.MoveNext(value))
                        {
                            taskScheduled[i] = null;
                        }
                        else
                        {
                            continue; // next i
                        }
                    }
                    catch (Exception ex)
                    {
                        taskScheduled[i] = null;
                        Debug.LogException(ex);
                    }
                }

                // find null, loop from tail
                while (i < j)
                {
                    var fromTail = taskScheduled[j];
                    if (fromTail != null)
                    {
                        try
                        {
                            if (!fromTail.MoveNext(value))
                            {
                                taskScheduled[j] = null;
                                j--;
                                continue; // next j
                            }
                            else
                            {
                                // swap
                                taskScheduled[i] = fromTail;
                                taskScheduled[j] = null;
                                j--;
                                goto NEXT_LOOP; // next i
                            }
                        }
                        catch (Exception ex)
                        {
                            taskScheduled[j] = null;
                            j--;
                            Debug.LogException(ex);
                            continue; // next j
                        }
                    }
                    else
                    {
                        j--;
                    }
                }

                tail = i; // loop end
                break; // LOOP END

                NEXT_LOOP:
                continue;
            }


            lock (runningAndQueueLock)
            {
                running = false;
                while (waitQueue.Count != 0)
                {
                    if (taskScheduled.Length == tail)
                    {
                        Array.Resize(ref taskScheduled, checked(tail * 2));
                    }
                    taskScheduled[tail++] = waitQueue.Dequeue();
                }
            }
        }
    }

    private sealed class Promise : IUniTaskSource<T>, ITaskPoolNode<Promise>
    {
        static TaskPool<Promise> pool;
        Promise? nextNode;
        public ref Promise NextNode
        {
            get
            {
                return ref nextNode!;
            }
        }

        static Promise()
        {
            TaskPool.RegisterSizeGetter(typeof(Promise), () => pool.Size);
        }

        public UniTaskCompletionSourceCore<T> core;
        CancellationToken cancellationToken;
        CancellationTokenRegistration cancellationTokenRegistration;
        bool cancelImmediately;

        Promise()
        {
        }

        public static IUniTaskSource<T> Create(Signal<T> runner, CancellationToken cancellationToken, bool cancelImmediately, out short token)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return AutoResetUniTaskCompletionSource<T>.CreateFromCanceled(cancellationToken, out token);
            }

            if (!pool.TryPop(out var result))
            {
                result = new Promise();
            }

            result.cancellationToken = cancellationToken;
            result.cancelImmediately = cancelImmediately;

            if (cancelImmediately && cancellationToken.CanBeCanceled)
            {
                result.cancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(state =>
                {
                    var promise = (Promise)state;
                    promise.core.TrySetCanceled(promise.cancellationToken);
                }, result);
            }

            TaskTracker.TrackActiveTask(result, 0);

            runner.AddAction(result);

            token = result.core.Version;
            return result;
        }

        public T GetResult(short token)
        {
            try
            {
                return core.GetResult(token);
            }
            finally
            {
                if (!(cancelImmediately && cancellationToken.IsCancellationRequested))
                {
                    TryReturn();
                }
                else
                {
                    TaskTracker.RemoveTracking(this);
                }
            }
        }

        void IUniTaskSource.GetResult(short token)
        {
            GetResult(token);
        }

        public UniTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        public UniTaskStatus UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        public bool MoveNext(T value)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                core.TrySetCanceled(cancellationToken);
                return false;
            }

            core.TrySetResult(value);
            return false;
        }

        public bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            core.Reset();
            cancellationToken = default;
            cancellationTokenRegistration.Dispose();
            return pool.TryPush(this);
        }
    }
}
