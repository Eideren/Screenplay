using System.Threading;

namespace Screenplay.Nodes
{
    public interface ICustomEntry : IScreenplayNode
    {
        void Run(IEventContext context, CancellationToken cancellation);
    }
}
