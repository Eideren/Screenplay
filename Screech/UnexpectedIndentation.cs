namespace Screech
{
    public class UnexpectedIndentation : Error
    {
        public UnexpectedIndentation(string? sourceLine, int sourceLineNumber, string text) : base(sourceLine, sourceLineNumber, text) { }
    }
}