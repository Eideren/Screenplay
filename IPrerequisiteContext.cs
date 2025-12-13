namespace Screenplay
{
    public interface IPrerequisiteContext
    {
        bool Visited(IPrerequisite executable);
    }
}
