namespace Screenplay.Nodes
{
    public interface ILocal : IScreenplayNode
    {
        public guid Id { get; set; }
        public bool AllowMultipleKeys { get; }
    }
}
