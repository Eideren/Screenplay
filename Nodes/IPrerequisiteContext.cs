namespace Screenplay.Nodes
{
    public interface IPrerequisiteContext
    {
        bool Visited(IPrerequisite executable);
    }
}
