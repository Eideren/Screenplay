using System.Threading;

namespace Screenplay
{
    public interface ICustomEntry : IScreenplayNode
    {
        void Run(IEventContext context, CancellationToken cancellation);
    }
}
