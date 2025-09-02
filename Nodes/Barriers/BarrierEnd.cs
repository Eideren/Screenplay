using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using YNode;

namespace Screenplay.Nodes.Barriers
{
    [NodeWidth(Barrier.Width)]
    public class BarrierEnd : AbstractScreenplayNode, IBarrierPart
    {
        [Output, SerializeReference]
        public IExe<IEventContext>? Next;

        [SerializeReference, ReadOnly, HideIf("@IBarrierPart.InNodeEditor")]
        public IPort[] InheritedPorts = Array.Empty<IPort>();

        IPort[] IBarrierPart.InheritedPorts => InheritedPorts;

        IBarrierPart? IBarrierPart.NextBarrier
        {
            get => null;
            set { }
        }

        public override void CollectReferences(List<GenericSceneObjectReference> references) { }

        public IEnumerable<IOutput> AllTracks()
        {
            yield break;
        }

        public void UpdatePorts(IBarrierPart parent)
        {
            var parentTracks = parent.AllTracks().ToArray();
            Array.Resize(ref InheritedPorts, parentTracks.Length);
            for (int i = 0; i < InheritedPorts.Length; i++)
            {
                parentTracks[i].ValidatePortType(ref InheritedPorts[i]);
                InheritedPorts[i].Parent = this;
            }
        }
    }
}
