using Sirenix.OdinInspector;

namespace Screenplay
{
    #warning should be reworked
    public class InterlocutorSource : AbstractScreenplayNode, IInterlocutorSource
    {
        [HideLabel]
        public required Interlocutor Interlocutor;

        public Interlocutor GetInterlocutor(IEventContext context) => Interlocutor;

        public override void CollectReferences(ReferenceCollector references) { }
    }
}
