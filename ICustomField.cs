using System;

namespace Screenplay
{
    public interface ICustomField { }

    public interface ICustomField<T> : ICustomField
    {
        public void GetValue(ScreenplayGraph graph, out T value, out Action<T>? onCleanup);
    }
}
