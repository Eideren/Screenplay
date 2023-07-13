namespace Screech
{
    public class UnknownScope : Error
    {
        public UnknownScope(string? sourceLine, int sourceLineNumber, string text) : base(sourceLine, sourceLineNumber, text) { }
    }
}