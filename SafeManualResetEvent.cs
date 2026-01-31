using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Screenplay
{
    public class SafeManualResetEvent
    {
        private readonly object _syncPrimitive = new();
        private readonly List<AutoResetUniTaskCompletionSource> _signals = new(4);
        private bool _closed = true;
        private CancellationToken? _cancelled;

        public UniTask AwaitOpen
        {
            get
            {
                lock (_syncPrimitive)
                {
                    if (_cancelled is { } cts)
                        throw new OperationCanceledException(cts);

                    if (_closed)
                    {
                        var autoResetCompletionSource = AutoResetUniTaskCompletionSource.Create();
                        _signals.Add(autoResetCompletionSource);
                        return autoResetCompletionSource.Task;
                    }
                    else
                    {
                        return UniTask.CompletedTask;
                    }
                }
            }
        }

        public UniTask AwaitClosed
        {
            get
            {
                lock (_syncPrimitive)
                {
                    if (_cancelled is { } cts)
                        throw new OperationCanceledException(cts);

                    if (_closed)
                    {
                        return UniTask.CompletedTask;
                    }
                    else
                    {
                        var autoResetCompletionSource = AutoResetUniTaskCompletionSource.Create();
                        _signals.Add(autoResetCompletionSource);
                        return autoResetCompletionSource.Task;
                    }
                }
            }
        }

        public bool TrySetCanceled(CancellationToken cts = default)
        {
            lock (_syncPrimitive)
            {
                if (_cancelled is not null)
                    return false;

                _cancelled = cts;

                int length = _signals.Count;
                var a = System.Buffers.ArrayPool<AutoResetUniTaskCompletionSource>.Shared.Rent(length);
                _signals.CopyTo(a);
                _signals.Clear();
                foreach (var signal in a.AsSpan()[..length])
                    signal.TrySetCanceled(cts);

                System.Buffers.ArrayPool<AutoResetUniTaskCompletionSource>.Shared.Return(a);

                return true;
            }
        }

        public void Open()
        {
            lock (_syncPrimitive)
            {
                if (_cancelled is not null)
                    return;

                _closed = false;
                Signal();
            }
        }

        public void Close()
        {
            lock (_syncPrimitive)
            {
                if (_cancelled is not null)
                    return;

                _closed = true;
                Signal();
            }
        }

        private void Signal()
        {
            int length = _signals.Count;
            var arr = System.Buffers.ArrayPool<AutoResetUniTaskCompletionSource>.Shared.Rent(length);
            _signals.CopyTo(arr);
            _signals.Clear();
            foreach (var signal in arr.AsSpan()[..length])
            {
                bool val = signal.TrySetResult();
                Debug.Assert(val);
            }

            System.Buffers.ArrayPool<AutoResetUniTaskCompletionSource>.Shared.Return(arr);
        }
    }
}
