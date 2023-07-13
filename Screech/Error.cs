namespace Screech
{
    public abstract class Error : Issue
    {
        protected Error(string? sourceLine, int sourceLineNumber, string text) : base(sourceLine, sourceLineNumber, text) { }
    }
}