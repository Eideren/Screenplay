using System.Threading;
using Cysharp.Threading.Tasks;

namespace Screenplay.Nodes.Barriers
{
    public interface IOutput
    {
        BarrierIntermediate? LoopsWithin { get; }
        IExecutable? Branch { get; }
        void CollectReferences(ReferenceCollector references);
        UniTask Execute(IEventContext context, CancellationToken cancellation);

        /// <summary> Check that the given output is setup in the same way as this one, aside for the branch it takes </summary>
        IOutput ValidateMatchingOutput(IOutput? output);
    }

    public static class ReferenceCollectorExtension
    {
        public static void Collect<T>(this ReferenceCollector @this, params T?[] outputs) where T : IOutput
        {
            foreach (T? output in outputs)
                output?.CollectReferences(@this);
        }
    }
}
