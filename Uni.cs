using System;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using Screenplay;
using UnityEngine;

public static class Uni
{
    public static UniTask NextFrame(Cancellation cancellation = default, bool cancelImmediately = true)
    {
        return new NextFramePromise(PlayerLoopTiming.Update, cancelImmediately).CreateTask(cancellation);
    }

    public static UniTask NextFrame(PlayerLoopTiming timing = PlayerLoopTiming.Update, Cancellation cancellation = default, bool cancelImmediately = true)
    {
        return new NextFramePromise(timing, cancelImmediately).CreateTask(cancellation);
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
        return new DelayPromise(delayType, delayTimeSpan, delayTiming, cancelImmediately).CreateTask(cancellation);
    }

    public static UniTask WaitUntilCanceled(Cancellation cancellationToken, PlayerLoopTiming? delayed = null)
    {
        return new WaitUntilCanceledPromise{ Delayed = delayed }.CreateTask(cancellationToken);
    }

    public static UniTask Yield(PlayerLoopTiming timing, Cancellation cancellationToken, bool cancelImmediately = false)
    {
        return new YieldPromise { Timing = timing, ImmediateCancellation = cancelImmediately }.CreateTask(cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static UniTask CreateTask<T>(this T promiseStruct, Cancellation cancellationToken) where T : struct, IPromiseStruct
    {
        return new UniTask(GenericPromise<T>.Create(promiseStruct, cancellationToken, out short token), token);
    }

    private interface IPromiseStruct
    {
        public PlayerLoopTiming? Timing { get; }
        public bool ImmediateCancellation { get; }
        public bool OnCancelThrow { get; }
        public bool ContinueWaiting();
    }

    private struct YieldPromise : IPromiseStruct
    {
        public required PlayerLoopTiming? Timing { get; set; }
        public required bool ImmediateCancellation { get; set; }
        public bool OnCancelThrow => true;
        public bool ContinueWaiting() => false;
    }

    private struct WaitUntilCanceledPromise : IPromiseStruct
    {
        PlayerLoopTiming? IPromiseStruct.Timing => Delayed;
        public required PlayerLoopTiming? Delayed { get; set; }
        public bool ImmediateCancellation => Delayed is null;
        public bool OnCancelThrow => false;
        public bool ContinueWaiting() => true;
    }

    private readonly struct NextFramePromise : IPromiseStruct
    {
        private readonly int _frameCount;
        private readonly PlayerLoopTiming _timing;

        public bool ImmediateCancellation { get; }

        public bool OnCancelThrow => true;

        PlayerLoopTiming? IPromiseStruct.Timing => _timing;

        public bool ContinueWaiting() => Time.frameCount < _frameCount;

        public NextFramePromise(PlayerLoopTiming timing, bool cancelImmediately)
        {
            _frameCount = Time.frameCount;
            _timing = timing;
            ImmediateCancellation = cancelImmediately;
        }
    }

    private struct DelayPromise : IPromiseStruct
    {
        private readonly DelayType _type;
        private readonly double _target;
        private readonly PlayerLoopTiming _timing;
        public bool ImmediateCancellation { get; set; }

        public bool OnCancelThrow => true;

        PlayerLoopTiming? IPromiseStruct.Timing => _timing;

        public bool ContinueWaiting()
        {
            return _type switch
            {
                DelayType.DeltaTime => Time.timeAsDouble < _target,
                DelayType.UnscaledDeltaTime => Time.unscaledTimeAsDouble < _target,
                _ => throw new ArgumentOutOfRangeException(nameof(_type), _type, null),
            };
        }

        public DelayPromise(DelayType type, double delayTimeSpan, PlayerLoopTiming timing, bool cancelImmediately)
        {
            _type = type;
            switch (_type)
            {
                case DelayType.DeltaTime:
                    _target = Time.timeAsDouble + delayTimeSpan;
                    break;
                case DelayType.UnscaledDeltaTime:
                    _target = Time.unscaledTimeAsDouble + delayTimeSpan;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            _timing = timing;
            ImmediateCancellation = cancelImmediately;
        }
    }

    private sealed class GenericPromise<T> : IUniTaskSource, IPlayerLoopItem, ITaskPoolNode<GenericPromise<T>> where T : struct, IPromiseStruct
    {
        private static TaskPool<GenericPromise<T>> s_pool;

        static GenericPromise()
        {
            TaskPool.RegisterSizeGetter(typeof(GenericPromise<T>), () => s_pool.Size);
        }

        private GenericPromise<T>? _nextNode;
        public ref GenericPromise<T> NextNode => ref _nextNode!;
        private UniTaskCompletionSourceCore<AsyncUnit> _core;
        private Cancellation _cancellation;
        private T _promiseStruct;

        private GenericPromise()
        {
        }

        public static IUniTaskSource Create(T promiseStruct, Cancellation cancellation, out short token)
        {
            AssertOnMainThread();

            if (cancellation.IsCancellationRequested)
            {
                return AutoResetUniTaskCompletionSource.CreateFromCanceled(cancellation.GetStandardToken(), out token);
            }

            if (!s_pool.TryPop(out var result))
            {
                result = new GenericPromise<T>();
            }

            result._promiseStruct = promiseStruct;
            result._cancellation = cancellation;

            if (result._promiseStruct.ImmediateCancellation)
                cancellation.Register(ImmediateCancellationHandler, result);

            TaskTracker.TrackActiveTask(result, 3);

            if (result._promiseStruct.Timing is {} timing)
                PlayerLoopHelper.AddAction(timing, result);

            token = result._core.Version;
            return result;
        }

        private static void ImmediateCancellationHandler(object state)
        {
            AssertOnMainThread();

            var promise = (GenericPromise<T>)state;
            if (promise._promiseStruct.OnCancelThrow)
                promise._core.TrySetCanceled(promise._cancellation.GetStandardToken());
            else
                promise._core.TrySetResult(default);
        }

        private static void AssertOnMainThread()
        {
            // Feel free to uncomment when debugging
            //Debug.Assert(PlayerLoopHelper.IsMainThread);
        }

        public bool MoveNext()
        {
            AssertOnMainThread();

            if (_cancellation.IsCancellationRequested)
            {
                if (_promiseStruct.OnCancelThrow)
                    _core.TrySetCanceled(_cancellation.GetStandardToken());
                else
                    _core.TrySetResult(default);
                return false;
            }

            if (_promiseStruct.ContinueWaiting())
                return true;

            _core.TrySetResult(default);
            return false;
        }

        public void GetResult(short token)
        {
            AssertOnMainThread();

            try
            {
                _core.GetResult(token);
            }
            finally
            {
                TaskTracker.RemoveTracking(this);
                if (_promiseStruct.ImmediateCancellation && _cancellation.IsCancellationRequested)
                {
                }
                else
                {
                    _core.Reset();
                    if (_promiseStruct.ImmediateCancellation)
                        _cancellation.Unregister(ImmediateCancellationHandler, this);
                    _promiseStruct = default;
                    _cancellation = default;
                    s_pool.TryPush(this);
                }
            }
        }

        public UniTaskStatus GetStatus(short token) => _core.GetStatus(token);

        public UniTaskStatus UnsafeGetStatus() => _core.UnsafeGetStatus();

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            AssertOnMainThread();
            _core.OnCompleted(continuation, state, token);
        }
    }
}
