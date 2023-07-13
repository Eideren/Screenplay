using System.Collections.Generic;

namespace Screech
{
    public abstract class NodeTree : Node
    {
        public List<Node>? Children { get; internal set; }
    }
}