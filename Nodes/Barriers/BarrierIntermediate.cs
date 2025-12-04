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
        [SerializeReference, HideIf("@IBarrierPart.InNodeEditor")]
        public required IBarrierPart? NextBarrier;

        [ListDrawerSettings(ShowFoldout = false, IsReadOnly = true, ShowItemCount = false, OnBeginListElementGUI = nameof(BeginDrawListElement), OnEndListElementGUI = nameof(EndDrawListElement)), SerializeReference]
        public IOutput[] InheritedTracks = Array.Empty<IOutput>();

        [SerializeReference, ListDrawerSettings(ShowFoldout = false, ShowItemCount = false, HideAddButton = true, OnEndListElementGUI = nameof(EndAdditionalDrawListElement)), OnCollectionChanged(nameof(UpdateNextPorts))]
        public required IOutput[] AdditionalTracks = Array.Empty<IOutput>();

        [SerializeReference, ReadOnly, HideIf("@IBarrierPart.InNodeEditor")]
        public Port[] InheritedPorts = Array.Empty<Port>();

        Port[] IBarrierPart.InheritedPorts => InheritedPorts;

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
                InheritedPorts[i] ??= new Port();
                InheritedPorts[i].Parent = this;
            }

            Array.Resize(ref InheritedTracks, parentTracks.Length);
            for (int i = 0; i < InheritedPorts.Length; i++)
                InheritedTracks[i] = parentTracks[i].ValidateMatchingOutput(InheritedTracks[i]);

            NextBarrier?.UpdatePorts(this);
        }

        public override void CollectReferences(ReferenceCollector references)
        {
            references.Collect(InheritedTracks);
            references.Collect(AdditionalTracks);
        }

        private void BeginDrawListElement(int index)
        {
            IBarrierPart.InheritedDrawBegin?.Invoke(this, index);
        }

        private void EndDrawListElement(int index)
        {
            IBarrierPart.InheritedDrawEnd?.Invoke(this, index);
        }

        private void EndAdditionalDrawListElement(int index)
        {
            IBarrierPart.AdditionalDrawEnd?.Invoke(this, index);
        }

        [Button(SdfIconType.Plus, Name = " ", DirtyOnClick = true)]
        public void AddTrack()
        {
            Array.Resize(ref AdditionalTracks, AdditionalTracks.Length+1);
            AdditionalTracks[^1] = new EventOutput();
            NextBarrier?.UpdatePorts(this);
        }
    }
}
