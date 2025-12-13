using System.Threading;
using Cysharp.Threading.Tasks;

namespace Screenplay
{
    /// <summary>
    /// Provides a way to trigger events under certain conditions
    /// </summary>
    public interface IPrecondition : IScreenplayNode
    {
        /// <summary>
        /// Set <see cref="tracker"/>'s <see cref="IPreconditionCollector.SetUnlockedState"/> when this case is triggered
        /// </summary>
        UniTask Setup(IPreconditionCollector tracker, CancellationToken triggerCancellation);
    }
}
