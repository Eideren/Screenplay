namespace Screenplay
{
    public interface INodeWithSceneGizmos : IScreenplayNode
    {
        void DrawGizmos(ref bool rebuildPreview);
    }
}
