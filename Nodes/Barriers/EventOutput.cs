using System;
using System.Collections.Generic;
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
        [SerializeReference, Output] public IExe<IEventContext>? Next;

        BarrierIntermediate? IOutput.LoopsWithin => LoopsWithin;

        IBranch? IOutput.Branch => Next;

        public UniTask Execute(IEventContext context, CancellationToken cancellation) => Next.Execute(context, cancellation);

        public void CollectReferences(List<GenericSceneObjectReference> references) { }

        public void ValidatePortType(ref IPort? port)
        {
            if (port is not Port<IEventContext>)
                port = new Port<IEventContext>();
        }

        public void ValidateMatchingOutput(ref IOutput? output)
        {
            if (output is not EventOutput)
                output = new EventOutput();
        }
    }
}
