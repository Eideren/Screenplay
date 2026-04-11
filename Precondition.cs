using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Screenplay
{
    /// <summary>
    /// Provides a way to trigger events under certain conditions
    /// </summary>
    [Serializable]
    public abstract class Precondition : AbstractScreenplayNode
    {
        /// <summary>
        /// Set <paramref name="tracker"/>'s <see cref="IPreconditionCollector.SetUnlockedState"/> when this case is triggered
        /// </summary>
        public abstract UniTask Setup(IPreconditionCollector tracker, Cancellation triggerCancellation);
    }
}
