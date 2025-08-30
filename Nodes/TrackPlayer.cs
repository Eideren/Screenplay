using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using YNode;

namespace Screenplay.Nodes
{
    public class TrackPlayer : ExecutableLinear
    {
        [SerializeReference, Input, Required] public Track? Track;
        [SerializeField, HideInInspector] public int From = -1, To = -1;

        public override void CollectReferences(List<GenericSceneObjectReference> references) => Track?.CollectReferences(references);

        protected override async UniTask LinearExecution(IEventContext context, CancellationToken cancellation)
        {
            if (Track == null)
            {
                Debug.LogWarning($"Unassigned {nameof(Track)}, skipping this {nameof(TrackPlayer)}");
                return;
            }

            await Track.RangePlayer(GetTimeSpan(Track), cancellation, false);
        }

        public override void FastForward(IEventContext context, CancellationToken cancellationToken)
        {
            if (Track == null)
            {
                Debug.LogWarning($"Unassigned {nameof(Track)}, skipping this {nameof(TrackPlayer)}");
                return;
            }

            var timespan = GetTimeSpan(Track);
            using var samplers = Track.GetDisposableSamplers();
            foreach (var sampler in samplers)
            {
                if (timespan.end >= sampler.start)
                    sampler.sampler.Sample(timespan.start, timespan.end);
            }
        }

        public override void SetupPreview(IPreviewer previewer, bool fastForwarded)
        {
            if (Track == null)
                return;

            foreach (var trackItem in Track.Items)
                trackItem?.AppendRollbackMechanism(previewer);

            if (fastForwarded)
                FastForward(previewer, CancellationToken.None);
            else
                previewer.PlaySafeAction(this);
        }

        protected (float start, float end) GetTimeSpan(Track track) => (GetMarker(true, From, track), GetMarker(false, To, track));

        private static float GetMarker(bool start, int id, Track track)
        {
            if (id == -1)
                return start ? 0f : track.Duration();

            if (id < 0 || id >= track.Markers.Length)
            {
                Debug.LogWarning($"TrackPlayer marker for {(start ? "start" : "end")} has unknown id '{id}' set, returning default range");
                return start ? 0f : track.Duration();
            }

            return track.Markers[id].Time;
        }
    }
}
