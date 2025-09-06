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

        public IPort ValidatePortType(IPort? port)
        {
            return port is not Port<IEventContext> ? new Port<IEventContext>() : port;
        }

        public IOutput ValidateMatchingOutput(IOutput? output)
        {
            return output is not EventOutput ? new EventOutput() : output;
        }
    }
}
