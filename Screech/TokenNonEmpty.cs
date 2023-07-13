#nullable enable

namespace Screech
{
    public class TokenNonEmpty : Issue
    {
        public TokenNonEmpty(string? sourceLine, int sourceLineNumber, string text) : base(sourceLine, sourceLineNumber, text) { }
    }
}