using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Screenplay.Nodes.Barriers
{
    [Serializable]
    public class Port : IExecutable
    {
        public const int Width = 24;
        public const int OffsetFromTop = 22;
        public const int HeightOfItems = 64 + Barrier.SpaceBetweenElements;
        public const int HeightOfPort = HeightOfItems;

        [HideInInspector, SerializeField, SerializeReference]
        public IBarrierPart Parent = null!;

        public Vector2 Position
        {
            get => Parent.Position + new Vector2(-Width, Array.IndexOf(Parent.InheritedPorts, this) * HeightOfItems + OffsetFromTop);
            set { }
        }

        public void CollectReferences(ReferenceCollector references) { }
        public void SetupPreview(IPreviewer previewer, bool fastForwarded) { }

        public IEnumerable<IExecutable?> Followup()
        {
            yield break;
        }

        public virtual UniTask InnerExecution(IEventContext context, CancellationToken cancellation)
        {
            return IBarrierPart.Group.NotifyReceivedGroup(cancellation, Parent);
        }

        public void FastForward(IEventContext context, CancellationToken cancellation) => throw new NotImplementedException();

        public void FastForwardEval(IEventContext context, FastForwardData data, CancellationToken cancellation, out UniTask? playbackStart)
        {
            // Sounds like we don't need to do anything here ? Need to validate
#warning unfinished
            throw new NotImplementedException();
        }
    }
}
