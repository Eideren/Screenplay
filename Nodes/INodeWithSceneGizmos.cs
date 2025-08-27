namespace Screenplay.Nodes
{
    public interface INodeWithSceneGizmos : IScreenplayNode
    {
        void DrawGizmos(ref bool rebuildPreview);
    }
}
