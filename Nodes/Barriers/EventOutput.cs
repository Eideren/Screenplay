using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using YNode;

namespace Screenplay.Nodes.Barriers
{
    [Serializable, InlineProperty]
    public class EventOutput : IOutput
    {
        [SerializeReference, Output] public BarrierIntermediate? LoopsWithin;
        [SerializeReference, Output] public IExecutable? Next;

        BarrierIntermediate? IOutput.LoopsWithin => LoopsWithin;

        IExecutable? IOutput.Branch => Next;

        public UniTask Execute(IEventContext context, CancellationToken cancellation) => Next.Execute(context, cancellation);

        public void CollectReferences(ReferenceCollector references) { }

        public IOutput ValidateMatchingOutput(IOutput? output)
        {
            return output is not EventOutput ? new EventOutput() : output;
        }
    }
}
