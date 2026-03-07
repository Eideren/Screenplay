using System.Threading;
using Cysharp.Threading.Tasks;

namespace Screenplay
{
    public interface IInterlocutorSource : IScreenplayNode
    {
        UniTask<IInterlocutor?> GetInterlocutor(IEventContext context, CancellationToken cancellationToken);
    }
}
