#nullable enable

namespace Screech
{
    public abstract class Issue
    {
        protected Issue(string? sourceLine, int sourceLineNumber, string text)
        {
            Text = $"{GetType().Name}: {text}";
            SourceLine = sourceLineNumber;
            LineContent = sourceLine;
        }

        public string Text { get; }
        public int SourceLine { get; }
        public string? LineContent { get; }
        public override string ToString() => $"'{Text}' line #{SourceLine}{(LineContent != null ? $": '{LineContent}'" : null)}";
    }
}