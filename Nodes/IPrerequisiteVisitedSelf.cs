namespace Screenplay.Nodes
{
    /// <summary>
    /// Satisfies the <see cref="IPrerequisite"/> interface by testing if 'this' has been visited
    /// </summary>
    public interface IPrerequisiteVisitedSelf : IPrerequisite
    {
        bool IPrerequisite.TestPrerequisite(IEventContext context) => context.Visited(this);
    }
}
