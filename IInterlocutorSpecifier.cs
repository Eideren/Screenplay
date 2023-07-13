namespace Screenplay
{
    /// <summary>
    /// A command which specifies who is performing the line this is bound to,
    /// see <see cref="IInterlocutor"/>
    /// </summary>
    public interface IInterlocutorSpecifier : ICommand
    {
        IInterlocutor GetInterlocutor();
    }
}