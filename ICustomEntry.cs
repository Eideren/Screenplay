namespace Screenplay
{
    public interface ICustomEntry : IScreenplayNode
    {
        void Run(IEventContext context, Cancellation cancellation);
    }
}
