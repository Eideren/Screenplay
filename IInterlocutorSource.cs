namespace Screenplay
{
    #warning rework this to have AI contribute
    public interface IInterlocutorSource : IScreenplayNode
    {
        Interlocutor GetInterlocutor(IEventContext context);
    }
}
