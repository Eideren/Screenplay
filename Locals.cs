using System.Collections.Generic;
using Screenplay.Nodes;
using UnityEngine;

namespace Screenplay
{
    public class Locals
    {
        private List<(ILocal id, guid value)> _values = new();

        public bool FindFirst(ILocal id, out guid value)
        {
            lock (_values)
            {
                foreach (var local in _values)
                {
                    if (local.id == id)
                    {
                        value = local.value;
                        return true;
                    }
                }

                value = default;
                return false;
            }
        }

        public bool TryGet(ILocal id, out guid value)
        {
            lock (_values)
            {
                foreach (var data in _values)
                {
                    if (data.id == id)
                    {
                        value = data.value;
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }

        public bool TryGet(ILocal id, List<guid> values)
        {
            bool any = false;
            lock (_values)
            {
                foreach (var local in _values)
                {
                    if (local.id == id)
                    {
                        any = true;
                        values.Add(local.value);
                    }
                }
            }

            return any;
        }

        public bool TryAdd((ILocal id, guid value) v)
        {
            lock (_values)
            {
                if (v.id.AllowMultipleKeys == false && _values.FindIndex(x => x.id == v.id) != -1)
                {
                    Debug.LogWarning($"Overlapping variants:{v.id}");
                    return false;
                }

                _values.Add(v);
                return true;
            }
        }

        public void Remove((ILocal id, guid value) v)
        {
            lock (_values)
            {
                _values.Remove(v);
            }
        }

        public void CopyTo(List<(ILocal id, guid value)> locals)
        {
            lock (_values)
            {
                foreach (var local in _values)
                    locals.Add(local);
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

        public (ILocal id, guid value)[] ToArray()
        {
            lock (_values)
            {
                return _values.ToArray();
            }
        }
    }
}
