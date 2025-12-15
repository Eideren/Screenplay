using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using YNode;

namespace Screenplay.Nodes.Barriers
{
    [NodeVisuals(Width = Barrier.Width)]
    public class BarrierEnd : AbstractScreenplayNode, IBarrierPart
    {
        [Output, SerializeReference]
        public IExecutable? Next;

        [SerializeReference, ReadOnly, HideIf("@IBarrierPart.InNodeEditor")]
        public Port[] InheritedPorts = Array.Empty<Port>();

        Port[] IBarrierPart.InheritedPorts => InheritedPorts;

        IBarrierPart? IBarrierPart.NextBarrier
        {
            get => null;
            set { }
        }

        public override void CollectReferences(ReferenceCollector references) { }

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
                InheritedPorts[i] ??= new Port();
                InheritedPorts[i].Parent = this;
            }
        }
    }
}
