namespace Screech
{
    public class TokenEmpty : Error
    {
        public TokenEmpty(string? sourceLine, int sourceLineNumber, string text) : base(sourceLine, sourceLineNumber, text) { }
    }
}