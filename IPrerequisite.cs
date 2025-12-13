namespace Screenplay
{
    /// <summary>
    /// This node can act as a prerequisite to run an <see cref="Nodes.Event"/>
    /// </summary>
    public interface IPrerequisite : IScreenplayNode
    {
        /// <summary> Whether this prerequisite is fulfilled </summary>
        bool TestPrerequisite(IEventContext context);
    }
}
