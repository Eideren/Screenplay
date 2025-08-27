using System.Threading;

namespace Screenplay.Nodes
{
    public interface ICustomEntry : IScreenplayNode
    {
        void Run(ScreenplayGraph graph, IEventContext context, CancellationToken cancellation);
    }
}
