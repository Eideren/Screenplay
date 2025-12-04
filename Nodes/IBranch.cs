using System.Collections.Generic;

namespace Screenplay.Nodes
{
    public interface IBranch : IScreenplayNode
    {
        /// <summary>
        /// The next action which will be played out, none if this is the last one,
        /// a single one for most <see cref="IExecutable"/>,
        /// multiple in case of branching through <see cref="Choice"/> for example.
        /// </summary>
        /// <remarks>
        /// Used to traverse the node tree, providing insight about nodes that are reachable
        /// </remarks>
        public IEnumerable<IBranch?> Followup();
    }
}
