using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using YNode;

namespace Screenplay.Nodes.Barriers
{
    [NodeWidth(Barrier.Width)]
    public class BarrierIntermediate : AbstractScreenplayNode, IBarrierPart
    {
        [SerializeReference, Required, HideIf("@IBarrierPart.InNodeEditor")]
        public IBarrierPart? NextBarrier;

        [ListDrawerSettings(ShowFoldout = false, IsReadOnly = true, ShowItemCount = false, OnEndListElementGUI = nameof(EndDrawListElement)), SerializeReference]
        public IOutput[] InheritedTracks = Array.Empty<IOutput>();

        [SerializeReference, Required, ListDrawerSettings(ShowFoldout = false, ShowItemCount = false, HideAddButton = true, OnEndListElementGUI = nameof(EndDrawListElement)), OnCollectionChanged(nameof(UpdateNextPorts))]
        public IOutput[] AdditionalTracks = Array.Empty<IOutput>();

        [SerializeReference, ReadOnly, HideIf("@IBarrierPart.InNodeEditor")]
        public IPort[] InheritedPorts = Array.Empty<IPort>();

        IPort[] IBarrierPart.InheritedPorts => InheritedPorts;

        IBarrierPart? IBarrierPart.NextBarrier
        {
            get => NextBarrier;
            set => NextBarrier = value ?? throw new NullReferenceException();
        }

        public IEnumerable<IOutput> AllTracks()
        {
            foreach (var output in InheritedTracks)
                yield return output;
            foreach (var output in AdditionalTracks)
                yield return output;
        }

        private void UpdateNextPorts()
        {
            NextBarrier?.UpdatePorts(this);
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

            Array.Resize(ref InheritedTracks, parentTracks.Length);
            for (int i = 0; i < InheritedPorts.Length; i++)
                parentTracks[i].ValidateMatchingOutput(ref InheritedTracks[i]);

            NextBarrier?.UpdatePorts(this);
        }

        public override void CollectReferences(List<GenericSceneObjectReference> references)
        {
            foreach (var track in InheritedTracks)
                track.CollectReferences(references);
            foreach (var track in AdditionalTracks)
                track.CollectReferences(references);
        }

        private void EndDrawListElement(int index)
        {
            GUILayout.Space(Barrier.SpaceBetweenElements);
        }
    }
}
