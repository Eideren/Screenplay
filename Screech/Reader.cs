using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Screech
{
    public class Reader : IEnumerator<FormattableString[]>
    {
        readonly List<(Choice choice, FormattableString content)> _choices = new();
        readonly Stack<(NodeTree tree, int currentIndex)> _stack = new();
        public bool IsChoice;

        public Reader(NodeTree root)
        {
            _stack.Push((root, 0));
        }

        public FormattableString[] Current { get; private set; } = Array.Empty<FormattableString>();
        FormattableString[] IEnumerator<FormattableString[]>.Current => Current;
        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_choices.Count > 0)
                throw new InvalidOperationException("Cannot call MoveNext after a choice without calling Choose beforehand");

            IsChoice = false;
            while (true)
            {
                if (_stack.Count == 0)
                    return false;

                if (_stack.Peek().currentIndex >= (_stack.Peek().tree.Children?.Count ?? 0))
                {
                    if (_stack.Peek().tree is Scope)
                        return false;
                    _stack.Pop();
                    continue;
                }

                (NodeTree tree, int currentIndex) tuple = _stack.Peek();
                NodeTree tree = tuple.tree;
                int currentIndex = tuple.currentIndex;
                Node currentToken = tree.Children[currentIndex];
                if (currentToken is not Scope)
                {
                    _stack.Pop();
                    _stack.Push((tree, currentIndex + 1));
                }

                if (currentToken is Line k)
                {
                    if (ShouldShow(k.Content, out FormattableString filteredContent))
                    {
                        List<Node> children = k.Children;
                        if (children != null && children.Count > 0)
                            _stack.Push((k, 0));
                        if (!string.IsNullOrWhiteSpace(filteredContent.Format))
                        {
                            Current = new FormattableString[1] { filteredContent };
                            return true;
                        }
                    }
                }
                else if (currentToken is Choice c)
                {
                    IsChoice = true;
                    _choices.Add((c, null));
                    (NodeTree tree2, int currentIndex2) = _stack.Peek();
                    for (; currentIndex2 < tree2.Children?.Count && tree2.Children[currentIndex2] is Choice otherC; currentIndex2++)
                        _choices.Add((otherC, null));
                    _stack.Pop();
                    _stack.Push((tree2, currentIndex2));
                    for (int j = _choices.Count - 1; j >= 0; j--)
                    {
                        Choice choice = _choices[j].choice;
                        if (ShouldShow(choice.Content, out FormattableString filteredContent2))
                            _choices[j] = (choice, filteredContent2);
                        else
                            _choices.RemoveAt(j);
                    }

                    if (_choices.Count != 0)
                    {
                        Current = new FormattableString[_choices.Count];
                        for (int i = 0; i < _choices.Count; i++)
                            Current[i] = _choices[i].content;
                        return true;
                    }
                }
                else if (currentToken is Return)
                {
                    NodeTree l;
                    do
                    {
                        if (_stack.Count == 0)
                            return false;
                        l = _stack.Peek().tree;
                        _stack.Pop();
                    } while (l is not Scope);
                }
                else if (currentToken is GoTo gt)
                    _stack.Push((gt.Destination, 0));
                else if (currentToken is Scope)
                    break;
                else if (currentToken is Comment) { }
                else {}
            }

            return false;
        }

        public void Reset()
        {
            while (_stack.Count > 1)
                _stack.Pop();
            (NodeTree, int) v = _stack.Pop();
            v.Item2 = 0;
            _stack.Push(v);
        }

        void IDisposable.Dispose() { }

        public void Choose(int indexChosen)
        {
            _stack.Push((_choices[indexChosen].choice, 0));
            _choices.Clear();
            IsChoice = false;
        }

        bool ShouldShow(FormattableString fs, [MaybeNullWhen(false)] out FormattableString fsFiltered)
        {
            bool hasDelegate = false;
            int max = fs.ArgumentCount;
            for (int j = 0; j < max; j++)
            {
                if (fs.GetArgument(j) is ShowWhen)
                {
                    hasDelegate = true;
                    break;
                }
            }

            if (!hasDelegate)
            {
                fsFiltered = fs;
                return true;
            }

            bool show = false;
            object[] args = fs.GetArguments();
            var toRemove = new List<(int, int)>();
            foreach (Match match in Script.InterpString.Matches(fs.Format))
            {
                if (match.Length != 0)
                {
                    Group content = match.Groups["index"];
                    if (args[int.Parse(content.Value)] is ShowWhen sw)
                    {
                        toRemove.Add((match.Index, match.Length));
                        show |= sw();
                    }
                }
            }

            if (!show)
            {
                fsFiltered = null;
                return false;
            }

            toRemove.Sort(((int s, int l) a, (int s, int l) b) => a.s.CompareTo(b.s));
            StringBuilder sb = new(fs.Format);
            for (int i = toRemove.Count - 1; i >= 0; i--)
            {
                (int s, int k) = toRemove[i];
                sb.Remove(s, k);
            }

            fsFiltered = FormattableStringFactory.Create(sb.ToString(), args);
            return true;
        }
    }
}