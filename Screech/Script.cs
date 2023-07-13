using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Screech
{
    public static class Script
    {
        const RegexOptions RegexOption = RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.ExplicitCapture;

        /// <summary>
        /// 'content' => {0,-10:FF}
        /// {'index','alignment':'format'}
        /// </summary>
        public static readonly Regex InterpString = new (@"(?<!{){\s*(?'content'(?'index'\d+)\s*(,\s*(?'alignment'(-\s*)?\d+))?\s*(:\s*(?'format'\w+))?\s*)}(?!})", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        public static readonly Regex Incrementation = new(@"^(?'increment'[ \t\u3000]*)", RegexOption);
        public static readonly Regex Scope = new(@"^[ \t\u3000]*=+[ \t\u3000]*(?'content'[^=\n]*)=*", RegexOption);
        public static readonly Regex GoTo = new(@"^[ \t\u3000]*->[ \t\u3000]*(?'content'[^\n]*)", RegexOption);
        public static readonly Regex Return = new(@"^[ \t\u3000]*<-[ \t\u3000]*(?'content'[^\n]*)", RegexOption);
        public static readonly Regex Choice = new(@"^[ \t\u3000]*>[ \t\u3000]*(?'content'[^\n]*)", RegexOption);
        public static readonly Regex CloseChoice = new(@"^[ \t\u3000]*<([^-]|$)[ \t\u3000]*(?'content'[^\n]*)", RegexOption);
        public static readonly Regex Comment = new(@"^[ \t\u3000]*//[ \t\u3000]*(?'content'[^\n]*)", RegexOption);
        public static readonly Regex Line = new(@"^[ \t\u3000]*(?'content'[^\n]*)", RegexOption);

        public static Scope Parse(FormattableString input, Action<Issue> issues, bool includeLineContentInIssue = true, bool stripComments = true)
        {
            var root = new Scope{ Name = "=> Root <=" };
            string format = input.Format.Replace("\r", "");
            format = InterpString.Replace(format, delegate(Match x)
            {
                Group group = x.Groups["index"];
                Group group2 = x.Groups["alignment"];
                Group group3 = x.Groups["format"];
                if (group3.Success && group2.Success)
                    return $"{{{group.Value},{group2.Value}:{group3.Value}}}";
                if (group2.Success)
                    return $"{{{group.Value},{group2.Value}}}";
                return group3.Success ? $"{{{group.Value}:{group3.Value}}}" : $"{{{group.Value}}}";
            });

            string incrementRuler = null;
            var scopes = new Dictionary<string, Scope>();
            var goTos = new List<(int, GoTo, string)>();
            var stack = new Stack<NodeTree>();
            stack.Push(root);

            string[] lines = format.Split('\n');
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                if (string.IsNullOrWhiteSpace(lines[lineIndex]))
                    continue;

                var globalToLocal = new Dictionary<string, (int index, object obj)>();
                var tempArgs = new List<object>();
                string line = InterpString.Replace(lines[lineIndex], delegate(Match m)
                {
                    if (m.Length == 0)
                        return m.Value;
                    string value = m.Groups["index"].Value;
                    if (!globalToLocal.TryGetValue(value, out (int, object) value2))
                    {
                        value2 = (tempArgs.Count, input.GetArgument(int.Parse(value)));
                        tempArgs.Add(value2.Item2);
                        globalToLocal.Add(value, value2);
                    }

                    return $"{{{value2.Item1}}}";
                });

                object[] args = tempArgs.ToArray();
                string lineOrNull = includeLineContentInIssue ? line : null;
                Match match = Incrementation.Match(line);
                string incrementation = match.Groups["increment"].Value;
                int depth = 0;
                if (incrementation.Length > 0)
                {
                    incrementRuler ??= incrementation;
                    bool isTab = incrementRuler[0] == '\t';
                    string newIncr = !isTab ? incrementation.Replace("\t", incrementRuler) : incrementation.Replace("    ", incrementRuler).Replace(" ", null);
                    int div = newIncr.Length / incrementRuler.Length;
                    depth += div;
                    if (newIncr != incrementation)
                        issues(new MixedIndentation(lineOrNull, lineIndex, $"Expected {(isTab ? "tabs" : "spaces")} but file contains mixed, this specific line contains {(isTab ? "spaces" : "tabs")}"));
                    if (depth > stack.Count)
                    {
                        issues(new UnexpectedIndentation(lineOrNull, lineIndex, $"Expected a depth of {stack.Count} at most, this one sits at {depth}"));
                        depth = stack.Count;
                    }
                    else if (newIncr.Length % incrementRuler.Length != 0)
                        issues(new UnexpectedIndentation(lineOrNull, lineIndex, $"Expected {div * incrementRuler.Length} or {(div + 1) * incrementRuler.Length} {(isTab ? "tabs" : "spaces")} but line contains {newIncr.Length}"));
                }

                while (depth < stack.Count - 1)
                    stack.Pop();

                if (depth == stack.Count)
                {
                    NodeTree parentParent = stack.Peek();
                    Node parent = parentParent.Children?[^1] ?? null;
                    if (parent is NodeTree tree)
                    {
                        bool valid = true;
                        if (tree is Line j)
                        {
                            valid = false;
                            for (int i = 0; i < j.Content.ArgumentCount; i++)
                            {
                                if (j.Content.GetArgument(i) is ShowWhen)
                                {
                                    valid = true;
                                    break;
                                }
                            }
                        }

                        if (valid)
                            stack.Push(tree);
                        else
                            issues(new UnexpectedIndentation(lineOrNull, lineIndex, $"Expected a '{typeof(ShowWhen)}' on parent line"));
                    }
                    else
                        issues(new UnexpectedIndentation(lineOrNull, lineIndex, "Indentation too deep or invalid parent line"));
                }

                if ((match = Scope.Match(line)).Success)
                {
                    string content6 = match.Groups["content"].Value;
                    if (!string.IsNullOrWhiteSpace(content6))
                    {
                        string name = content6.Trim();
                        Scope scope = new()
                        {
                            Name = name
                        };
                        if ((root.Children == null || root.Children.Count == 0) && scopes.Count == 0)
                            root = scope;
                        scopes.Add(name, scope);
                        while (stack.Count != 0)
                            stack.Pop();
                        stack.Push(scope);
                    }
                    else
                        issues(new TokenEmpty(lineOrNull, lineIndex, "Scope must be named"));
                }
                else if ((match = GoTo.Match(line)).Success)
                {
                    string content5 = match.Groups["content"].Value;
                    if (!string.IsNullOrWhiteSpace(content5))
                    {
                        string destination = content5.Trim();
                        GoTo g = new();
                        goTos.Add((lineIndex, g, destination));
                        NodeTree nodeTree = stack.Peek();
                        (nodeTree.Children ?? (nodeTree.Children = new List<Node>())).Add(g);
                    }
                    else
                        issues(new TokenEmpty(lineOrNull, lineIndex, "GoTo must have a destination"));
                }
                else if ((match = Return.Match(line)).Success)
                {
                    string content4 = match.Groups["content"].Value;
                    if (string.IsNullOrWhiteSpace(content4))
                    {
                        NodeTree nodeTree = stack.Peek();
                        (nodeTree.Children ?? (nodeTree.Children = new List<Node>())).Add(new Return());
                    }
                    else
                        issues(new TokenNonEmpty(lineOrNull, lineIndex, "Return must not have any content on the same line"));
                }
                else if ((match = Choice.Match(line)).Success)
                {
                    string contentFormat = match.Groups["content"].Value.Trim();
                    Choice choice = new()
                    {
                        Content = FormattableStringFactory.Create(contentFormat, args)
                    };
                    NodeTree nodeTree = stack.Peek();
                    (nodeTree.Children ?? (nodeTree.Children = new List<Node>())).Add(choice);
                }
                else if ((match = CloseChoice.Match(line)).Success)
                {
                    string content3 = match.Groups["content"].Value;
                    if (string.IsNullOrWhiteSpace(content3))
                    {
                        NodeTree nodeTree = stack.Peek();
                        (nodeTree.Children ?? (nodeTree.Children = new List<Node>())).Add(new CloseChoice());
                    }
                    else
                        issues(new TokenNonEmpty(lineOrNull, lineIndex, "Force close choice must not have any content on the same line"));
                }
                else if ((match = Comment.Match(line)).Success)
                {
                    if (!stripComments)
                    {
                        string content2 = match.Groups["content"].Value;
                        NodeTree nodeTree = stack.Peek();
                        (nodeTree.Children ?? (nodeTree.Children = new List<Node>())).Add(new Comment
                        {
                            Text = FormattableStringFactory.Create(content2, args)
                        });
                    }
                }
                else
                {
                    match = Line.Match(line);
                    string content = match.Groups["content"].Value;
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        NodeTree nodeTree = stack.Peek();
                        (nodeTree.Children ?? (nodeTree.Children = new List<Node>())).Add(new Line
                        {
                            Content = FormattableStringFactory.Create(content.Trim(), args)
                        });
                    }
                }
            }

            foreach ((int line2, GoTo goTo, string dest) in goTos)
            {
                if (scopes.TryGetValue(dest, out Scope scopeDest))
                    goTo.Destination = scopeDest;
                else
                    issues(new UnknownScope(includeLineContentInIssue ? lines[line2] : null, line2, $"Could not find scope '{dest}' in file"));
            }

            TrimLists(root);
            foreach (KeyValuePair<string, Scope> item in scopes)
                TrimLists(item.Value);
            return root;

            static void TrimLists(NodeTree t)
            {
                if (t.Children == null)
                    return;
                t.Children.TrimExcess();
                foreach (Node child in t.Children)
                {
                    if (child is NodeTree st)
                        TrimLists(st);
                }
            }
        }
    }
}