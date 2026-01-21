using System.Threading;
using Cysharp.Threading.Tasks;

namespace Screenplay
{
    public interface IInterlocutorSource : IScreenplayNode
    {
        UniTask<Interlocutor?> GetInterlocutor(IEventContext context, CancellationToken cancellationToken);
    }
}
