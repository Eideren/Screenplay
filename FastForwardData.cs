using System.Collections.Generic;

namespace Screenplay
{
    public class FastForwardData
    {
        public readonly Queue<IScreenplayNode?> _queue;

        public FastForwardData(Queue<IScreenplayNode?> queue)
        {
            _queue = queue;
        }

        public bool TryPopIfMatch<T>(T? param) where T : class
        {
            return TryPopMatch(new[] { param }, out _);
        }

        public bool TryPopMatch<T>(IEnumerable<T?> param, out T? result) where T : class
        {
            if (_queue.TryPeek(out var next))
            {
                foreach (var executable in param)
                {
                    if (ReferenceEquals(executable, next))
                    {
                        _queue.Dequeue();
                        // Cases covered:
                        // Next is null -> one of the branches is null as well, meaning that this null was likely the one taken
                        // Next is not null and one of the branches goes to this value, meaning that this branch was previously taken
                        result = executable;
                        return true;
                    }
                }
            }

            result = null;
            return false;
        }
    }
}
