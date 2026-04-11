using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Screenplay
{
    public interface IExecutable : IBranch, IPrerequisite, IPreviewable
    {
        /// <inheritdoc cref="IBranch.Followup"/>
        new IEnumerable<IExecutable?> Followup();

        /// <summary>
        /// Run the barebone logic for this action, return the next action to run
        /// </summary>
        UniTask<IExecutable?> Execute(IEventContext context, Cancellation cancellation);

        /// <summary>
        /// Applies all the changes <see cref="Execute"/> introduces to the objects it touches,
        /// and ensures those objects have those changes re-instated when they are loaded again.
        /// </summary>
        /// <remarks>
        /// Is called by the saving system on each node traversed in the saved session to reload that session's state
        /// </remarks>
        UniTask Persistence(IEventContext context, Cancellation cancellation);

        /// <inheritdoc />
        bool IPrerequisite.TestPrerequisite(IEventContext context) => context.Visited(this);

        /// <inheritdoc/>
        IEnumerable<IBranch?> IBranch.Followup() => Followup();
    }
}
