using System;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using Screenplay;
using UnityEngine;

public static class UniTaskExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UniTask NextFrame(Cancellation cancellation = default, bool cancelImmediately = true)
    {
        return NextFrame(PlayerLoopTiming.Update, cancellation:cancellation, cancelImmediately: cancelImmediately);
    }

    public static UniTask NextFrame(PlayerLoopTiming timing = PlayerLoopTiming.Update, Cancellation cancellation = default, bool cancelImmediately = true)
    {
        return new UniTask(NextFramePromise.Create(timing, cancellation, cancelImmediately, out var token), token);
    }

    public static UniTask Delay(double delayTimeSpan, DelayType delayType = DelayType.DeltaTime, PlayerLoopTiming delayTiming = PlayerLoopTiming.Update, Cancellation cancellation = default, bool cancelImmediately = true)
    {
        if (delayTimeSpan < 0)
            throw new ArgumentOutOfRangeException("Delay does not allow minus delayTimeSpan. delayTimeSpan:" + delayTimeSpan);

#if UNITY_EDITOR
        // force use Realtime.
        if (PlayerLoopHelper.IsMainThread && !UnityEditor.EditorApplication.isPlaying)
            delayType = DelayType.Realtime;
#endif
        return new UniTask(DelayPromise.Create(delayTimeSpan, delayTiming, cancellation, cancelImmediately, delayType, out var token), token);
    }

    public static UniTask WaitUntilCanceled(Cancellation cancellationToken, PlayerLoopTiming? delayed = null)
    {
        return new UniTask(WaitUntilCanceledPromise.Create(cancellationToken, delayed, out var token), token);
    }

    public static UniTask Yield(PlayerLoopTiming timing, Cancellation cancellationToken, bool cancelImmediately = false)
    {
        return new UniTask(YieldPromise.Create(timing, cancellationToken, cancelImmediately, out var token), token);
    }

    sealed class NextFramePromise : IUniTaskSource, IPlayerLoopItem, ITaskPoolNode<NextFramePromise>
    {
        private static TaskPool<NextFramePromise> pool;
        private static Action<object> s_action;
        NextFramePromise? nextNode;
        public ref NextFramePromise NextNode => ref nextNode!;

        static NextFramePromise()
        {
            TaskPool.RegisterSizeGetter(typeof(NextFramePromise), () => pool.Size);
        }

        int frameCount;
        UniTaskCompletionSourceCore<AsyncUnit> core;
        Cancellation cancellation;
        bool cancelImmediately;

        NextFramePromise()
        {
        }

        public static IUniTaskSource Create(PlayerLoopTiming timing, Cancellation cancellation, bool cancelImmediately, out short token)
        {
            if (cancellation.IsCancellationRequested)
            {
                return AutoResetUniTaskCompletionSource.CreateFromCanceled(cancellation.GetStandardToken(), out token);
            }

            if (!pool.TryPop(out var result))
            {
                result = new NextFramePromise();
            }

            result.frameCount = PlayerLoopHelper.IsMainThread ? Time.frameCount : -1;
            result.cancellation = cancellation;
            result.cancelImmediately = cancelImmediately;

            if (result.cancelImmediately)
                cancellation.Register(ImmediateCancellationHandler, result);

            TaskTracker.TrackActiveTask(result, 3);

            PlayerLoopHelper.AddAction(timing, result);

            token = result.core.Version;
            return result;
        }

        public void GetResult(short token)
        {
            try
            {
                core.GetResult(token);
            }
            finally
            {
                if (!(cancelImmediately && cancellation.IsCancellationRequested))
                {
                    TryReturn();
                }
                else
                {
                    TaskTracker.RemoveTracking(this);
                }
            }
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

        public bool MoveNext()
        {
            if (cancellation.IsCancellationRequested)
            {
                core.TrySetCanceled(cancellation.GetStandardToken());
                return false;
            }

            if (frameCount == Time.frameCount)
            {
                return true;
            }

            core.TrySetResult(AsyncUnit.Default);
            return false;
        }

        bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            core.Reset();
            if (cancelImmediately)
                cancellation.Unregister(ImmediateCancellationHandler, this);
            cancellation = default;
            return pool.TryPush(this);
        }

        private static void ImmediateCancellationHandler(object state)
        {
            var promise = (NextFramePromise)state;
            promise.core.TrySetCanceled(promise.cancellation.GetStandardToken());
        }
    }

    sealed class DelayPromise : IUniTaskSource, IPlayerLoopItem, ITaskPoolNode<DelayPromise>
    {
        static TaskPool<DelayPromise> pool;
        DelayPromise nextNode;
        public ref DelayPromise NextNode => ref nextNode;

        static DelayPromise()
        {
            TaskPool.RegisterSizeGetter(typeof(DelayPromise), () => pool.Size);
        }

        double target;
        Cancellation cancellation;
        bool cancelImmediately;
        DelayType delayType;

        UniTaskCompletionSourceCore<object> core;

        DelayPromise()
        {
        }

        public static IUniTaskSource Create(double delayTimeSpan, PlayerLoopTiming timing, Cancellation cancellation, bool cancelImmediately, DelayType delayType, out short token)
        {
            if (cancellation.IsCancellationRequested)
            {
                return AutoResetUniTaskCompletionSource.CreateFromCanceled(cancellation.GetStandardToken(), out token);
            }

            if (!pool.TryPop(out var result))
            {
                result = new DelayPromise();
            }

            switch (delayType)
            {
                case DelayType.DeltaTime:
                    result.target = Time.timeAsDouble + delayTimeSpan;
                    break;
                case DelayType.UnscaledDeltaTime:
                    result.target = Time.unscaledTimeAsDouble + delayTimeSpan;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(delayType), delayType, null);
            }
            result.cancellation = cancellation;
            result.cancelImmediately = cancelImmediately;

            if (cancelImmediately)
                cancellation.Register(ImmediateCancellationHandler, result);

            TaskTracker.TrackActiveTask(result, 3);

            PlayerLoopHelper.AddAction(timing, result);

            token = result.core.Version;
            return result;
        }

        public void GetResult(short token)
        {
            try
            {
                core.GetResult(token);
            }
            finally
            {
                if (!(cancelImmediately && cancellation.IsCancellationRequested))
                {
                    TryReturn();
                }
                else
                {
                    TaskTracker.RemoveTracking(this);
                }
            }
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

        public bool MoveNext()
        {
            if (cancellation.IsCancellationRequested)
            {
                core.TrySetCanceled(cancellation.GetStandardToken());
                return false;
            }

            bool r = delayType switch
            {
                DelayType.DeltaTime => Time.timeAsDouble >= target,
                DelayType.UnscaledDeltaTime => Time.unscaledTimeAsDouble >= target,
                _ => throw new ArgumentOutOfRangeException(nameof(delayType), delayType, null),
            };
            if (r)
            {
                core.TrySetResult(null);
                return false;
            }

            return true;
        }

        bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            core.Reset();
            target = default;
            if (cancelImmediately)
                cancellation.Unregister(ImmediateCancellationHandler, this);
            cancellation = default;
            cancelImmediately = default;
            delayType = default;
            return pool.TryPush(this);
        }

        private static void ImmediateCancellationHandler(object state)
        {
            var promise = (DelayPromise)state;
            promise.core.TrySetCanceled(promise.cancellation.GetStandardToken());
        }
    }

    sealed class WaitUntilCanceledPromise : IUniTaskSource, IPlayerLoopItem, ITaskPoolNode<WaitUntilCanceledPromise>
    {
        static TaskPool<WaitUntilCanceledPromise> pool;
        WaitUntilCanceledPromise nextNode;
        public ref WaitUntilCanceledPromise NextNode => ref nextNode;

        static WaitUntilCanceledPromise()
        {
            TaskPool.RegisterSizeGetter(typeof(WaitUntilCanceledPromise), () => pool.Size);
        }

        Cancellation cancellationToken;
        bool cancelImmediately;

        UniTaskCompletionSourceCore<object> core;

        WaitUntilCanceledPromise()
        {
        }

        public static IUniTaskSource Create(Cancellation cancellationToken, PlayerLoopTiming? delayed, out short token)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return AutoResetUniTaskCompletionSource.CreateFromCanceled(cancellationToken.GetStandardToken(), out token);
            }

            if (!pool.TryPop(out var result))
            {
                result = new WaitUntilCanceledPromise();
            }

            result.cancellationToken = cancellationToken;
            result.cancelImmediately = delayed is null;

            if (result.cancelImmediately && cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(ImmediateCancellationHandler, result);
            }

            TaskTracker.TrackActiveTask(result, 3);

            if (delayed is {} timing)
                PlayerLoopHelper.AddAction(timing, result);

            token = result.core.Version;
            return result;
        }

        public void GetResult(short token)
        {
            try
            {
                core.GetResult(token);
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

        public bool MoveNext()
        {
            if (cancellationToken.IsCancellationRequested)
            {
                core.TrySetResult(null);
                return false;
            }

            return true;
        }

        bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            core.Reset();
            if (cancelImmediately)
                cancellationToken.Unregister(ImmediateCancellationHandler, this);
            cancellationToken = default;
            cancelImmediately = default;
            return pool.TryPush(this);
        }

        private static void ImmediateCancellationHandler(object state)
        {
            var promise = (WaitUntilCanceledPromise)state;
            promise.core.TrySetResult(null);
        }
    }

    sealed class YieldPromise : IUniTaskSource, IPlayerLoopItem, ITaskPoolNode<YieldPromise>
    {
        static TaskPool<YieldPromise> pool;
        YieldPromise nextNode;
        public ref YieldPromise NextNode => ref nextNode;

        static YieldPromise()
        {
            TaskPool.RegisterSizeGetter(typeof(YieldPromise), () => pool.Size);
        }

        Cancellation cancellationToken;
        bool cancelImmediately;
        UniTaskCompletionSourceCore<object> core;

        YieldPromise()
        {
        }

        public static IUniTaskSource Create(PlayerLoopTiming timing, Cancellation cancellationToken, bool cancelImmediately, out short token)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return AutoResetUniTaskCompletionSource.CreateFromCanceled(cancellationToken.GetStandardToken(), out token);
            }

            if (!pool.TryPop(out var result))
            {
                result = new YieldPromise();
            }

            result.cancellationToken = cancellationToken;
            result.cancelImmediately = cancelImmediately;

            if (cancelImmediately && cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(ImmediateCancellationHandler, result);
            }

            TaskTracker.TrackActiveTask(result, 3);

            PlayerLoopHelper.AddAction(timing, result);

            token = result.core.Version;
            return result;
        }

        public void GetResult(short token)
        {
            try
            {
                core.GetResult(token);
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

        public bool MoveNext()
        {
            if (cancellationToken.IsCancellationRequested)
            {
                core.TrySetCanceled(cancellationToken.GetStandardToken());
                return false;
            }

            core.TrySetResult(null);
            return false;
        }

        bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            core.Reset();
            if (cancelImmediately)
                cancellationToken.Unregister(ImmediateCancellationHandler, this);
            cancellationToken = default;
            cancelImmediately = default;
            return pool.TryPush(this);
        }

        private static void ImmediateCancellationHandler(object state)
        {
            var promise = (YieldPromise)state;
            promise.core.TrySetCanceled(promise.cancellationToken.GetStandardToken());
        }
    }
}
