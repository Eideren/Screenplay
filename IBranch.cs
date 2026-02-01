using System.Collections.Generic;
using Screenplay.Nodes;

namespace Screenplay
{
    public interface IBranch : IScreenplayNode
    {
        /// <summary>
        /// The next actions which this node may branch into,
        /// should return null if any of those leads to the end of the path.
        /// </summary>
        /// <remarks>
        /// Used to restore a screenplay's state from a saved session
        /// </remarks>
        public IEnumerable<IBranch?> Followup();
    }
}
