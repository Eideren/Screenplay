namespace Screech
{
    public class GoTo : Node
    {
        public Scope Destination { get; internal set; } = null;
        public override string ToString() => $"-> {Destination.Name}";
    }
}