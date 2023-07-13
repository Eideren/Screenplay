using System;
using System.Collections;
using System.Collections.Generic;
using Screenplay;
using Object = UnityEngine.Object;

public static class IValidatableExtension
{
    static readonly EmptyEnumerable _empty = new();
    public static IEnumerable<(string name, IValidatable validatable)> NoSubValues(this IValidatable this_) => _empty;

    /// <summary>
    /// Return a collection containing sub values recursively, handles infinite recursion properly
    /// </summary>
    public static HashSet<IValidatable> GetAllSubValues(this IValidatable this_)
    {
        var checkedSet = new HashSet<IValidatable>();
        var leftToCheck = new Queue<IValidatable>();
        leftToCheck.Enqueue(this_);
        checkedSet.Add(this_);
        while (leftToCheck.Count > 0)
        {
            IValidatable current = leftToCheck.Dequeue();
            foreach ((string name, IValidatable validatable) e in current.GetSubValues())
            {
                if (checkedSet.Add(e.validatable))
                    leftToCheck.Enqueue(e.validatable);
            }
        }

        return checkedSet;
    }

    /// <summary>
    /// Go through this object and its hierarchy to validate them all,
    /// will throw an <see cref="ValidationException"/> with the appropriate data if it is invalid
    /// </summary>
    public static void ValidateAll(this IValidatable this_)
    {
        var checkedSet = new HashSet<IValidatable>();
        var leftToCheck = new Queue<IValidatable>();
        leftToCheck.Enqueue(this_);
        checkedSet.Add(this_);
        while (leftToCheck.Count > 0)
        {
            IValidatable current = leftToCheck.Dequeue();
            try
            {
                current.ValidateSelf();
            }
            catch (Exception e2)
            {
                PathTo(this_, current, new HashSet<IValidatable>(), out string path, out Object closestObj);
                throw new ValidationException(path, closestObj, e2);
            }

            foreach ((string name, IValidatable validatable) e in current.GetSubValues())
            {
                if (e.validatable == null)
                {
                    PathTo(this_, current, new HashSet<IValidatable>(), out string path, out Object closestObj);
                    throw new ValidationException(path, closestObj, new Exception($"{current.GetType().Name}.{e.name} is null"));
                }

                if (checkedSet.Add(e.validatable))
                    leftToCheck.Enqueue(e.validatable);
            }
        }
    }

    static bool PathTo(IValidatable node, IValidatable query, HashSet<IValidatable> done, out string localPath, out Object closestObj)
    {
        if (!done.Add(node))
        {
            localPath = null;
            closestObj = null;
            return false;
        }

        if (node == null && query == null)
        {
            localPath = "null";
            closestObj = null;
            return true;
        }

        if (node == query)
        {
            closestObj = node as Object;
            localPath = GetNameFor(node);
            return true;
        }

        foreach ((string name, IValidatable validatable) e in node.GetSubValues())
        {
            if (PathTo(e.validatable, query, done, out localPath, out Object c))
            {
                localPath = $"{GetNameFor(node)} / {e.name} / {localPath}";
                closestObj = c != null ? c : node as Object;
                return true;
            }
        }

        localPath = null;
        closestObj = null;
        return false;

        static string GetNameFor(IValidatable v)
        {
            if (v is Object o && o != null)
                return o.ToString();
            return v.GetType().Name;
        }
    }

    class EmptyEnumerable : IEnumerable<(string name, IValidatable validatable)>
    {
        readonly EmptyEnumerator _empty = new();
        public IEnumerator<(string name, IValidatable validatable)> GetEnumerator() => _empty;
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        class EmptyEnumerator : IEnumerator<(string name, IValidatable validatable)>
        {
            public (string name, IValidatable validatable) Current { get; }
            object IEnumerator.Current => Current;
            public bool MoveNext() => false;
            public void Reset() { }
            public void Dispose() { }
        }
    }
}