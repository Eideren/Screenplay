namespace Screenplay
{
    public interface INodeWithSceneGizmos : IScreenplayNode
    {
        void DrawGizmos(SceneGUIProxy guiProxy, ScreenplayGraph graph, ref bool rebuildPreview);
    }
}
