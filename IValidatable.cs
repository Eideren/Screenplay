using System.Collections.Generic;

namespace Screenplay
{
    /// <summary>
    /// An object which requires a specific set up through the unity inspector before usage,
    /// those methods should be implemented to throw if fields or parameters of the object are not in the right state for usage
    /// </summary>
    public interface IValidatable
    {
        /// <summary>
        /// Are the fields local to this class properly set up, throw if not.
        /// </summary>
        void ValidateSelf();

        /// <summary>
        /// Return fields contained in this object which implement <see cref="IValidatable"/>
        /// so that the caller may validate those as well.
        /// </summary>
        IEnumerable<(string name, IValidatable validatable)> GetSubValues();
    }
}