using System;

namespace Screech
{
    public class Choice : NodeTree
    {
        public FormattableString Content { get; internal set; } = null;
        public override string ToString() => $"> {Content.Format}";
    }
}