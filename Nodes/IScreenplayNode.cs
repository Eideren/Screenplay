using YNode;

namespace Screenplay.Nodes
{
    public interface IScreenplayNode : INodeValue
    {
        /// <summary>
        /// Appends this node's cross-scene references to the list.
        /// </summary>
        /// <remarks>
        /// Used to mark objects that are referenced by a <see cref="ScreenplayGraph"/> in the scene.
        /// </remarks>
        void CollectReferences(ReferenceCollector references);
    }
}
