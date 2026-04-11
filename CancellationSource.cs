using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

namespace Screenplay
{
    public class CancellationSource
    {
        private int _canceled;
        private CancellationTokenSource? _cts;
        private ConditionalWeakTable<object, Action<object>> _onCancel = new();
        private CancellationTokenRegistration _ctr;

        public CancellationSource() { }

        /// <summary> When <paramref name="parent"/> is canceled, so will this one be </summary>
        public CancellationSource(Cancellation parent) => parent.Register(this);

        public static CancellationSource CreateLinkedTokenSource(Cancellation parent) => new(parent);

        public static CancellationSource CreateLinkedTokenSource(Cancellation parentA, Cancellation parentB)
        {
            var source = new CancellationSource();
            parentA.Register(source);
            parentB.Register(source);
            return source;
        }

        public bool IsCancellationRequested => _canceled > 0;

        public Cancellation Token => new(this);

        public CancellationToken GetStandardToken()
        {
            lock (_onCancel)
            {
                if (IsCancellationRequested)
                    return new CancellationToken(true);

                _cts ??= new CancellationTokenSource();
                return _cts.Token;
            }
        }

        public void Cancel()
        {
            if (Interlocked.CompareExchange(ref _canceled, 1, 0) != 0)
                return;

            lock (_onCancel)
            {
                _cts?.Cancel();
                _cts?.Dispose();
                foreach (var (param, action) in _onCancel)
                {
                    try
                    {
                        action(param);
                    }
                    catch(Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
                _onCancel.Clear();
            }
        }

        public void Register(Action<object> action, object parameter)
        {
            lock (_onCancel)
            {
                if (IsCancellationRequested)
                {
                    try
                    {
                        action(parameter);
                    }
                    catch(Exception e)
                    {
                        Debug.LogException(e);
                    }
                    return;
                }

                _onCancel.Add(parameter, action);
            }
        }

        public void Unregister(Action<object> action, object parameter)
        {
            lock (_onCancel)
            {
                if (IsCancellationRequested)
                {
                    try
                    {
                        action(parameter);
                    }
                    catch(Exception e)
                    {
                        Debug.LogException(e);
                    }
                    return;
                }

                _onCancel.Remove(parameter);
            }
        }

        public void Register(Action action)
        {
            Register(static o => ((Action)o).Invoke(), action);
        }

        public void Unregister(Action action)
        {
            Unregister(static o => ((Action)o).Invoke(), action);
        }

        public void Register(CancellationSource source)
        {
            Register(static o => ((CancellationSource)o).Cancel(), source);
        }
    }
}
