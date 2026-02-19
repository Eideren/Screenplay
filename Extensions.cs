using System.Collections.Generic;

namespace Screenplay
{
    public static class Extensions
    {
        public static IEnumerable<T> NotNull<T>(this IEnumerable<T?> enumm)
        {
            foreach (var v in enumm)
            {
                if (v is not null)
                    yield return v;
            }
        }
    }
}
