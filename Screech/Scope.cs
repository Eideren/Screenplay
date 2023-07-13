namespace Screech
{
    public class Scope : NodeTree
    {
        public string Name { get; internal set; } = null;
        public override string ToString() => $"== {Name} ==";
    }
}