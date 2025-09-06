using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using YNode;
using Screenplay.Nodes.Barriers;

namespace Screenplay.Nodes
{
    [NodeWidth(Width)]
    public class Barrier : AbstractScreenplayNode, IExe<IEventContext>, IPrerequisiteVisitedSelf, IBarrierPart
    {
        public const int Width = NodeWidthAttribute.Default;
        public const int SpaceBetweenElements = 100;

        [SerializeReference, HideIf("@IBarrierPart.InNodeEditor")]
        public IBarrierPart NextBarrier = null!;

        [SerializeReference, Required, ListDrawerSettings(ShowFoldout = false, ShowItemCount = false, HideAddButton = true, OnEndListElementGUI = nameof(EndDrawListElement))]
        public IOutput[] Tracks = { new EventOutput() };

        public IPort[] InheritedPorts => Array.Empty<IPort>();

        IBarrierPart? IBarrierPart.NextBarrier
        {
            get => NextBarrier;
            set => NextBarrier = value ?? throw new NullReferenceException();
        }

        void IBarrierPart.UpdatePorts(IBarrierPart parent) => NextBarrier.UpdatePorts(this);

        public IEnumerable<IOutput> AllTracks() => Tracks;

        public override void CollectReferences(List<GenericSceneObjectReference> references)
        {
            foreach (var output in Tracks)
                output.CollectReferences(references);
        }

        public IEnumerable<IBranch?> Followup()
        {
            foreach (var output in Tracks)
                yield return output.Branch;
        }

        public void SetupPreview(IPreviewer previewer, bool fastForwarded)
        {

        }

        public async UniTask InnerExecution(IEventContext context, CancellationToken cancellation)
        {
            BarrierEnd barrierEnd;
            using (new IBarrierPart.Group(context, cancellation, this, out var completionTask))
            {
                barrierEnd = await completionTask;
            }

            await barrierEnd.Next.Execute(context, cancellation);
        }

        public void FastForwardEval(IEventContext context, FastForwardData data, CancellationToken cancellation, out UniTask? playbackStart)
        {
#warning unfinished
            throw new NotImplementedException();
        }

        public BarrierEnd? End()
        {
            for (IBarrierPart? part = this; part != null; part = part.NextBarrier)
            {
                if (part is BarrierEnd e)
                    return e;
            }

            return null;
        }

        private void EndDrawListElement(int index)
        {
            GUILayout.Space(SpaceBetweenElements);
        }

        [Button(SdfIconType.Plus, Name = " ", DirtyOnClick = true)]
        public void AddTrack()
        {
            Array.Resize(ref Tracks, Tracks.Length+1);
            Tracks[^1] = new EventOutput();
            NextBarrier.UpdatePorts(this);
        }
    }
}
