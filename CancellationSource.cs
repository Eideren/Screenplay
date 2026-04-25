using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Screenplay
{
    public class CancellationSource
    {
        private int _canceled;
        private CancellationTokenSource? _cts;
#warning this is problematic, I'm supposed to keep a hold of *some* of these, otherwise they are GCed
        private Dictionary<object, Action<object>> _onCancel = new();
        private CancellationTokenRegistration _ctr;
        private string? mn, fp;
        public int ln;

        public CancellationSource([CallerMemberName] string? mn = null, [CallerLineNumber] int ln = -1, [CallerFilePath] string? fp = null)
        {
            this.mn = mn;
            this.fp = fp;
            this.ln = ln;
        }

        /// <summary> When <paramref name="parent"/> is canceled, so will this one be </summary>
        public CancellationSource(Cancellation parent, [CallerMemberName] string? mn = null, [CallerLineNumber] int ln = 0, [CallerFilePath] string? fp = null)
            : this(mn, ln, fp)
        {
            parent.AlsoCancels(this);
        }

        public static CancellationSource CreateLinkedTokenSource(Cancellation parent, [CallerMemberName] string? mn = null, [CallerLineNumber] int ln = 0, [CallerFilePath] string? fp = null)
        {
            return new CancellationSource(parent, mn, ln, fp);
        }

        public static CancellationSource CreateLinkedTokenSource(Cancellation parentA, Cancellation parentB, [CallerMemberName] string? mn = null, [CallerLineNumber] int ln = 0, [CallerFilePath] string? fp = null)
        {
            var source = new CancellationSource(mn, ln, fp);
            parentA.AlsoCancels(source);
            parentB.AlsoCancels(source);
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

            Debug.Assert(PlayerLoopHelper.IsMainThread);

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
                    catch (Exception e)
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
                    catch (Exception e)
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
                    catch (Exception e)
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

        public void AlsoCancel(CancellationSource source)
        {
            Register(static o => ((CancellationSource)o).Cancel(), source);
        }
    }
}
