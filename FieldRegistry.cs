using System;
using System.Collections.Generic;

namespace Screenplay
{
    public class FieldRegistry : IDisposable
    {
        private readonly ScreenplayGraph _graph;
        private readonly Dictionary<Type, (object obj, Action? cleanup)> _values = new();

        public FieldRegistry(ScreenplayGraph graph)
        {
            _graph = graph;
        }

        public T2 Get<T, T2>() where T : ICustomField<T2>
        {
            if (_values.TryGetValue(typeof(T), out var obj))
                return (T2)obj.obj;

            foreach (var field in _graph.Fields)
            {
                if (field is T t)
                {
                    t.GetValue(_graph, out var value, out var cleanup);
                    Action? a = cleanup is null ? null : () => cleanup(value);
                    _values.Add(typeof(T), (value!, a));
                    return value;
                }
            }

            throw new Exception($"Could not find field {typeof(T)} on {_graph}");
        }

        public void Dispose()
        {
            foreach (var valueTuple in _values)
                valueTuple.Value.cleanup?.Invoke();
            _values.Clear();
        }
    }
}
