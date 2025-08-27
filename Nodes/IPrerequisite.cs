namespace Screenplay.Nodes
{
    /// <summary>
    /// This node can act as a prerequisite to run an <see cref="Event"/>
    /// </summary>
    public interface IPrerequisite : IScreenplayNode
    {
        /// <summary> Whether this prerequisite is fulfilled </summary>
        bool TestPrerequisite(IEventContext context);
    }
}
