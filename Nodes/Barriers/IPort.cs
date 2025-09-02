using YNode;

namespace Screenplay.Nodes.Barriers
{
    public interface IPort : INodeValue
    {
        public IBarrierPart Parent { set; }
    }
}
