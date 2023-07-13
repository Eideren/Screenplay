using System;
using Object = UnityEngine.Object;

namespace Screenplay
{
    /// <summary> The <see cref="IValidatable"/> at <see cref="Path"/> was not in a valid state </summary>
    public class ValidationException : Exception
    {
        public readonly Object ClosestObj;
        public readonly string Path;

        public ValidationException(string message, Object closestObj) : base(message)
        {
            ClosestObj = closestObj;
        }

        public ValidationException(string path, Object closestObj, Exception innerException) : base($"Data for '{path}' is not set properly, {innerException.GetType().Name}:{innerException.Message}", innerException)
        {
            Path = path;
            ClosestObj = closestObj;
        }
    }
}