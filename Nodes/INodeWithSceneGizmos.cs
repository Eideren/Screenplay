namespace Screenplay.Nodes
{
    public interface INodeWithSceneGizmos : IScreenplayNodeValue
    {
        void DrawGizmos(ref bool rebuildPreview);
    }
}
