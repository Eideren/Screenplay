using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Screenplay.Nodes.TrackItems;
using Sirenix.OdinInspector;
using UnityEngine;
using YNode;

namespace Screenplay.Nodes
{
    [Serializable, NodeVisuals(0.25f, 0.25f, 0.25f, Width = 800)]
    public class Track : AbstractScreenplayNode, IPreviewable, INodeWithSceneGizmos
    {
        [SerializeReference, InlineProperty]
        public ITrackItem?[] Items = Array.Empty<ITrackItem?>();
        public Marker[] Markers = Array.Empty<Marker>();

        [NonSerialized]
        public float DebugPlayHead;
        [NonSerialized]
        public PreviewMode DebugScrub = PreviewMode.Scrub;

        public float Duration()
        {
            float duration = 0;
            foreach (var trackItem in Items)
            {
                if (trackItem is not null && trackItem.Timespan.end > duration)
                    duration = trackItem.Timespan.end;
            }

            return duration;
        }

        public async UniTask RangePlayer((float start, float end) timespan, CancellationToken cancellation, bool loop)
        {
            using var samplers = GetDisposableSamplers();
            float t = timespan.start;
            float previousT = t;
            do
            {
                t += Time.deltaTime;
                if (loop && t >= timespan.end)
                    t -= timespan.end - timespan.start;

                foreach (var sampler in samplers)
                {
                    if (t >= sampler.start)
                        sampler.sampler.Sample(previousT, t);
                }

                previousT = t;
                await UniTask.NextFrame(cancellation, cancelImmediately:true);
            } while (loop || t < timespan.end);
        }

        public override void CollectReferences(ReferenceCollector references) => references.Collect(Items);

        public void SetupPreview(IPreviewer previewer, bool fastForwarded)
        {
            foreach (var trackItem in Items)
                trackItem?.AppendRollbackMechanism(previewer);

            previewer.AddCustomPreview(Preview);
            return;

            async UniTask Preview(CancellationToken cancellation)
            {
                var timespan = (start:0f, end:Duration());
                using var samplers = GetDisposableSamplers();

                float previousT = DebugPlayHead;
                do
                {
                    if (DebugScrub != PreviewMode.Scrub)
                        DebugPlayHead += Time.deltaTime;

                    foreach (var sampler in samplers)
                    {
                        if (DebugPlayHead >= sampler.start)
                            sampler.sampler.Sample(previousT, DebugPlayHead);
                    }

                    previousT = DebugPlayHead;
                    await UniTask.NextFrame(cancellation, cancelImmediately:true);
                    if (DebugScrub != PreviewMode.Scrub && previewer.Loop && DebugPlayHead >= timespan.end)
                        DebugPlayHead -= timespan.end;

                } while (DebugScrub == PreviewMode.Scrub || DebugPlayHead < timespan.end);
                // ReSharper disable once IteratorNeverReturns
            }
        }

        public SamplersList GetDisposableSamplers()
        {
            var samplers = new SamplersList(Items.Length);
            try
            {
                foreach (var trackItem in Items)
                {
                    var sampler = trackItem?.TryGetSampler();
                    if (sampler != null)
                        samplers.Add((sampler, trackItem!.Timespan.start));
                }
            }
            catch
            {
                foreach (var sampler in samplers)
                    sampler.sampler.Dispose();
            }

            return samplers;
        }

        public class SamplersList : List<(ITrackSampler sampler, float start)>, IDisposable
        {
            public SamplersList(int capacity) : base(capacity) { }

            public void Dispose()
            {
                foreach (var sampler in this)
                    sampler.sampler.Dispose();
            }
        }

        [Serializable]
        public struct Marker
        {
            [HorizontalGroup] public string? Name;
            [HorizontalGroup] public float Time;
        }

        public enum PreviewMode
        {
            Scrub,
            Play
        }

        public void DrawGizmos(ref bool rebuildPreview)
        {
            foreach (var item in Items)
            {
                if (item is TransformTrackItem tti)
                    tti.DrawGizmos(ref rebuildPreview);
            }
        }
    }
}
