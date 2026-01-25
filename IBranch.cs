using System.Collections.Generic;
using Screenplay.Nodes;

namespace Screenplay
{
    public interface IBranch : IScreenplayNode
    {
        /// <summary>
        /// The next action which will be played out, none if this is the last one,
        /// a single one for most <see cref="Choice"/>,
        /// multiple in case of branching through <see cref="IExecutable"/> for example.
        /// </summary>
        /// <remarks>
        /// Used to traverse the node tree, providing insight about nodes that are reachable
        /// </remarks>
        public IEnumerable<IBranch> Followup();
    }
}
