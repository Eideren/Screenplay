using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Screenplay.Nodes
{
    public interface IBranch : IScreenplayNode
    {
        /// <summary>
        /// The next action which will be played out, none if this is the last one,
        /// a single one for most <see cref="IExecutable{T}"/>,
        /// multiple in case of branching through <see cref="Choice"/> for example.
        /// </summary>
        /// <remarks>
        /// Used to traverse the node tree, providing insight about nodes that are reachable
        /// </remarks>
        public IEnumerable<IBranch?> Followup();
    }

    public interface IExe<TContext> : IBranch, IPrerequisite, IPreviewable where TContext : IExecutableContext<TContext>
    {
        /// <summary>
        /// Run the core logic of this action, use <see cref="IExecutableExtensions.Execute{T}"/> instead !
        /// </summary>
        UniTask InnerExecution(TContext context, CancellationToken cancellation);

        /// <summary>
        /// Fast-forward this branch's execution until we reach the end of <paramref name="data"/>,
        /// <paramref name="playbackStart"/> will be assigned to the node when data ran out, the node which we should resume from.
        /// </summary>
        void FastForwardEval(TContext context, FastForwardData data, CancellationToken cancellation, out UniTask? playbackStart);
    }

    public interface IExecutable<TContext> : IExe<TContext> where TContext : IExecutableContext<TContext>
    {
        /// <inheritdoc/>
        IEnumerable<IBranch?> IBranch.Followup() => Followup();

        /// <inheritdoc cref="IBranch.Followup"/>
        new IEnumerable<IExe<TContext>?> Followup();

        /// <summary>
        /// Run <see cref="IExe{T}.InnerExecution"/> to completion in a single call,
        /// for an animation node, this would apply the last frame of the animation for example
        /// </summary>
        void FastForward(TContext context, CancellationToken cancellation);

        /// <inheritdoc/>
        void IExe<TContext>.FastForwardEval(TContext context, FastForwardData data, CancellationToken cancellation, out UniTask? playbackStart)
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
        public static async UniTask Execute<TContext>(this IExe<TContext>? action, TContext context, CancellationToken cancellation)
            where TContext : IExecutableContext<TContext>
        {
            context.Visiting(action);
            if (action != null)
                await action.InnerExecution(context, cancellation);
        }
    }

    public interface IExecutableContext<TSelf> where TSelf : IExecutableContext<TSelf>
    {
        void Visiting(IExe<TSelf>? executable);
    }
}
