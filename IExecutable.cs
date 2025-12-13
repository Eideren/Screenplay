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
        /// Run the core logic of this action, use <see cref="IExecutableExtensions.Execute"/> instead !
        /// </summary>
        UniTask InnerExecution(IEventContext context, CancellationToken cancellation);

        /// <summary>
        /// Run <see cref="IExecutable.InnerExecution"/> to completion in a single call,
        /// for an animation node, this would apply the last frame of the animation for example
        /// </summary>
        void FastForward(IEventContext context, CancellationToken cancellation);

        /// <inheritdoc />
        bool IPrerequisite.TestPrerequisite(IEventContext context) => context.Visited(this);

        /// <inheritdoc/>
        IEnumerable<IBranch?> IBranch.Followup() => Followup();

        /// <summary>
        /// Fast-forward this branch's execution until we reach the end of <paramref name="data"/>,
        /// <paramref name="playbackStart"/> will be assigned to the node when data ran out, the node which we should resume from.
        /// </summary>
        void FastForwardEval(IEventContext context, FastForwardData data, CancellationToken cancellation, out UniTask? playbackStart)
        {
            if (data.TryPopMatch(Followup(), out var found))
            {
                FastForward(context, cancellation);
                context.Visiting(found);
                if (found is not null)
                    found.FastForwardEval(context, data, cancellation, out playbackStart);
                else
                    playbackStart = null;
                return;
            }

            playbackStart = this.Execute(context, cancellation);
        }
    }

    public static class IExecutableExtensions
    {
        /// <summary>
        /// Execute the given node and notify the context for visitation
        /// </summary>
        public static async UniTask Execute(this IExecutable? action, IEventContext context, CancellationToken cancellation)
        {
            context.Visiting(action);
            if (action != null)
                await action.InnerExecution(context, cancellation);
        }
    }
}
