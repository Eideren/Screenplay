using System.Collections.Generic;

namespace Screenplay
{
    public interface IUntypedGlobalsDeclarer : IScreenplayNode
    {
        /// <summary> Guid for this declarer </summary>
        guid Guid { get; set; }

        /// <summary>
        /// Return ids for each value this object declares.
        /// Were <see cref="ValueIds"/>[i] can be used as input in <see cref="TryGetValue"/> to retrieve a value
        /// </summary>
        IEnumerable<guid> ValueIds { get; }

        /// <summary> Try to get the actual value associated to a guid returned by <see cref="ValueIds"/> </summary>
        bool TryGetValue(guid valueId, out object? value);

        /// <summary> Fallback for previews </summary>
        object? GetDefault(out guid valueId);
    }

    public interface IProxyForUntypedGlobalsDeclarer : IUntypedGlobalsDeclarer
    {
        /// <summary> The Proxy to forward calls to </summary>
        IUntypedGlobalsDeclarer ProxyTarget { get; }

        /// <inheritdoc/>
        guid IUntypedGlobalsDeclarer.Guid
        {
            get => ProxyTarget.Guid;
            set => ProxyTarget.Guid = value;
        }

        /// <inheritdoc/>
        IEnumerable<guid> IUntypedGlobalsDeclarer.ValueIds => ProxyTarget.ValueIds;

        /// <inheritdoc/>
        bool IUntypedGlobalsDeclarer.TryGetValue(guid valueId, out object? value) => ProxyTarget.TryGetValue(valueId, out value);

        /// <inheritdoc/>
        object? IUntypedGlobalsDeclarer.GetDefault(out guid valueId) => ProxyTarget.GetDefault(out valueId);
    }

    public interface IGlobalsDeclarer<T> : IUntypedGlobalsDeclarer
    {
        /// <inheritdoc cref="IUntypedGlobalsDeclarer.TryGetValue"/>
        bool TryGetValue(guid valueId, out T val);

        /// <inheritdoc cref="IUntypedGlobalsDeclarer.GetDefault"/>
        new T GetDefault(out guid guid);

        /// <inheritdoc/>
        bool IUntypedGlobalsDeclarer.TryGetValue(guid valueId, out object? value)
        {
            if (TryGetValue(valueId, out var o))
            {
                value = o;
                return true;
            }

            value = null;
            return false;
        }

        /// <inheritdoc/>
        object? IUntypedGlobalsDeclarer.GetDefault(out guid valueId) => GetDefault(out valueId);
    }

    public interface IProxyForGlobalsDeclarer<T> : IGlobalsDeclarer<T>, IProxyForUntypedGlobalsDeclarer
    {
        /// <inheritdoc cref="IProxyForUntypedGlobalsDeclarer.ProxyTarget"/>
        new IGlobalsDeclarer<T> ProxyTarget { get; }

        /// <inheritdoc/>
        guid IUntypedGlobalsDeclarer.Guid
        {
            get => ProxyTarget.Guid;
            set => ProxyTarget.Guid = value;
        }

        /// <inheritdoc/>
        IUntypedGlobalsDeclarer IProxyForUntypedGlobalsDeclarer.ProxyTarget => ProxyTarget;

        /// <inheritdoc/>
        IEnumerable<guid> IUntypedGlobalsDeclarer.ValueIds => ProxyTarget.ValueIds;

        /// <inheritdoc cref="IUntypedGlobalsDeclarer.GetDefault"/>
        object? IUntypedGlobalsDeclarer.GetDefault(out guid valueId) => ProxyTarget.GetDefault(out valueId);

        /// <inheritdoc cref="IUntypedGlobalsDeclarer.TryGetValue"/>
        bool IUntypedGlobalsDeclarer.TryGetValue(guid valueId, out object? val) => ProxyTarget.TryGetValue(valueId, out val);

        /// <inheritdoc/>
        bool IGlobalsDeclarer<T>.TryGetValue(guid valueId, out T val) => ProxyTarget.TryGetValue(valueId, out val);

        /// <inheritdoc/>
        T IGlobalsDeclarer<T>.GetDefault(out guid guid) => ProxyTarget.GetDefault(out guid);
    }
}
