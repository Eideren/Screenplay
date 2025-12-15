using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;

namespace Screenplay.Nodes
{
    #warning should be reworked
    public class InterlocutorSource : AbstractScreenplayNode, IInterlocutorSource
    {
        [HideLabel]
        public required Interlocutor Interlocutor;

        public UniTask<Interlocutor> GetInterlocutor(IEventContext context, CancellationToken cancellationToken) => new(Interlocutor);

        public override void CollectReferences(ReferenceCollector references) { }
    }
}
