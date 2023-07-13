namespace Screech
{
    public class MixedIndentation : Issue
    {
        public MixedIndentation(string? sourceLine, int sourceLineNumber, string text) : base(sourceLine, sourceLineNumber, text) { }
    }
}