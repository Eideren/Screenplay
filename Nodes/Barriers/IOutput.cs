using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Screenplay.Nodes.Barriers
{
    public interface IOutput
    {
        BarrierIntermediate? LoopsWithin { get; }
        IBranch? Branch { get; }
        void CollectReferences(List<GenericSceneObjectReference> references);
        UniTask Execute(IEventContext context, CancellationToken cancellation);

        /// <summary> Check that the given port can receive this output, if not, create an instance that can </summary>
        void ValidatePortType(ref IPort? port);

        /// <summary> Check that the given output is setup in the same way as this one, aside for the branch it takes </summary>
        void ValidateMatchingOutput(ref IOutput? output);
    }
}
