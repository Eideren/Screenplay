using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Screenplay.Nodes
{
    /// <summary>
    /// A node that can affect the game state in some way
    /// </summary>
    public interface IExecutable : IPrerequisite, IPreviewable
    {
        /// <summary>
        /// The next action which will be played out, none if this is the last one,
        /// a single one for most <see cref="IExecutable"/>,
        /// multiple in case of branching through <see cref="Choice"/> for example.
        /// </summary>
        /// <remarks>
        /// Used to traverse the node tree, providing insight about nodes that are reachable
        /// </remarks>
        IEnumerable<IExecutable> Followup();

        /// <summary>
        /// Run the core logic of this action, use <see cref="IExecutableExtensions.Execute"/> instead !
        /// </summary>
        Awaitable InnerExecution(IContext context, CancellationToken cancellation);

        /// <summary>
        /// Execute this action to completion in a single call, applying any side effects the <see cref="InnerExecution"/> would have introduced if it ran in its stead
        /// </summary>
        /// <remarks>
        /// Used when loading game saves, calling this over each node that have been visited in the save.
        /// </remarks>
        void FastForward(IContext context);
    }

    public static class IExecutableExtensions
    {
        public static async Awaitable Execute(this IExecutable? action, IContext context, CancellationToken cancellation)
        {
            if (action != null)
            {
                context.Visiting.Add(action);
                await action.InnerExecution(context, cancellation);
            }
        }
    }
}
