using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Screenplay
{
    /// <summary>
    /// A field which accepts both unity scriptable objects and components while filtering based on if they implement a given interface
    /// </summary>
    [Serializable] public struct UInterface<T> where T : class
    {
        [SerializeField] Object UnityObj;

        public UInterface(T value)
        {
            UnityObj = value as Object ?? throw new InvalidOperationException($"{nameof(value)} of type {value.GetType()} should inherit from {typeof(Object)}");
        }

        public T Reference
        {
            get => UnityObj as T;
            set => this = new UInterface<T>(value);
        }
    }
}