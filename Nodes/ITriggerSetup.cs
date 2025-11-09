using System.Threading;
using Cysharp.Threading.Tasks;

namespace Screenplay.Nodes
{
    /// <summary>
    /// Takes care of setting up new triggers in the game world.
    /// For example, adding an interaction point in the world triggering the event linked.
    /// </summary>
    public interface ITriggerSetup : IScreenplayNode
    {
        /// <summary>
        /// Await for this trigger to signal and optionally return some annotation for other systems to retrieve within the same branch
        /// </summary>
        UniTask<IAnnotation?> AwaitTrigger(CancellationToken cancellation);
    }
}
