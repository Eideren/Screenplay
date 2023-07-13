using System;

namespace Screech
{
    public class Line : NodeTree
    {
        public FormattableString Content { get; internal set; } = null;
        public override string ToString() => Content.Format;
    }
}