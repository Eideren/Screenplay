namespace Screenplay.Nodes.TrackItems
{
    public interface ITrackItem
    {
        string Label { get; }
        float Start { get; set; }
        float Duration { get; set; }
        (float start, float end) Timespan => (Start, Start + Duration);
        void CollectReferences(ReferenceCollector references);
        ITrackSampler? TryGetSampler();
        void AppendRollbackMechanism(IPreviewer previewer);
    }

    public static class ReferenceCollectorExtension
    {
        public static void Collect<T>(this ReferenceCollector @this, params T?[] outputs) where T : ITrackItem
        {
            foreach (T? output in outputs)
                output?.CollectReferences(@this);
        }
    }
}
