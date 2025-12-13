using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Screenplay
{
    public class Locals
    {
        private readonly List<GlobalId> _values = new();

        public bool TryFind<T>(IGlobalsDeclarer<T> declarer, out guid id, [MaybeNullWhen(false)]out T value)
        {
            lock (_values)
            {
                foreach (var local in _values)
                {
                    if (local.DeclarerGuid == declarer.Guid && declarer.TryGetValue(local.ValueGuid, out value))
                    {
                        id = local.ValueGuid;
                        return true;
                    }
                }

                value = default;
                id = default;
                return false;
            }
        }

        public T FindWithFallback<T>(IGlobalsDeclarer<T> declarer, out guid id, bool logWarningOnFallback = true)
        {
            if (TryFind(declarer, out id, out var value))
            {
                return value;
            }

            if (logWarningOnFallback)
                Debug.LogWarning($"Using fallback for {declarer}");

            return declarer.GetDefault(out id);
        }

        public bool TryAdd(GlobalId v)
        {
            lock (_values)
            {
                /*if (v.Declarer.AllowMultipleKeys == false && _values.FindIndex(x => x.Declarer == v.Declarer) != -1)
                {
                    Debug.LogWarning($"Overlapping variants:{v.Declarer}");
                    return false;
                }*/

                _values.Add(v);
                return true;
            }
        }

        public void Remove(GlobalId v)
        {
            lock (_values)
            {
                _values.Remove(v);
            }
        }

        public void CopyTo(Locals dest)
        {
            lock (_values)
            {
                lock (dest)
                {
                    foreach (var local in _values)
                        dest.TryAdd(local);
                }
            }
        }

        public void Clear()
        {
            lock (_values)
            {
                _values.Clear();
            }
        }

        public GlobalId[] ToArray()
        {
            lock (_values)
            {
                return _values.ToArray();
            }
        }
    }
}
