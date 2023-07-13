using System;

namespace Screech
{
    public class Comment : Node
    {
        public FormattableString Text { get; internal set; } = null;
        public override string ToString() => $"// {Text.Format}";
    }
}