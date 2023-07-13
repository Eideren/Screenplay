using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Screech
{
    public class CompoundString
    {
        public const char ContentMarker = '\u200b';
        public const string ContentMarkerString = "\u200b";
        public List<Content> Contents;
        public string Text;

        public CompoundString(FormattableString formattableString)
        {
            Contents = new List<Content>(formattableString.ArgumentCount);
            object[] objs = formattableString.GetArguments();
            string str = formattableString.Format;
            str = str.Replace(ContentMarkerString, "");
            Text = Script.InterpString.Replace(str, delegate(Match match)
            {
                int num = int.Parse(GetSpanFromGroup(match.Groups["index"], str));
                int? alignment = null;
                string format = null;
                if (match.Groups["alignment"] is Group g && g.Success)
                    alignment = int.Parse(GetSpanFromGroup(g, str));
                if (match.Groups["format"] is Group g2 && g2.Success)
                    format = GetSpanFromGroup(g2, str).ToString();

                Contents.Add(new Content
                {
                    Object = objs[num],
                    Alignment = alignment,
                    Format = format
                });
                return ContentMarkerString;

                static ReadOnlySpan<char> GetSpanFromGroup(Group group, string baseString)
                {
                    ReadOnlySpan<char> readOnlySpan = baseString.AsSpan();
                    int index = group.Index;
                    int length = group.Index + group.Length - index;
                    return readOnlySpan.Slice(index, length);
                }
            });
            Contents.TrimExcess();
        }

        public bool NextContent(int start, out int indexInString)
        {
            for (indexInString = start; indexInString < Text.Length; indexInString++)
            {
                if (Text[indexInString] == ContentMarker)
                    return true;
            }

            return false;
        }

        public void InsertInStringAt(int indexInString, Content content)
        {
            int contentIndex = FindInsertionPointForContent(indexInString);
            Text = Text.Insert(indexInString, ContentMarkerString);
            Contents.Insert(contentIndex, content);
        }

        public int FindInsertionPointForContent(int indexInString)
        {
            int contentIndex = 0;
            for (int i = 0; i < indexInString; i++)
            {
                if (Text[i] == ContentMarker)
                    contentIndex++;
            }

            return contentIndex;
        }

        public FormattableString ToFormattableString()
        {
            object[] args = new object[Contents.Count];
            for (int j = 0; j < Contents.Count; j++)
                args[j] = Contents[j].Object;

            string formattedString = Text;
            for (int i = formattedString.Length - 1, c = Contents.Count - 1; i >= 0; i--)
            {
                if (formattedString[i] == ContentMarker)
                {
                    formattedString = formattedString.Remove(i, 1);
                    string format = $"{{{c}";
                    if (Contents[c].Alignment.HasValue)
                        format += $",{Contents[c].Alignment}";
                    if (Contents[c].Format != null)
                        format = $"{format}:{Contents[c].Format}";
                    format += "}";
                    formattedString = formattedString.Insert(i, format);
                    c--;
                }
            }

            return FormattableStringFactory.Create(formattedString, args);
        }

        public static bool IsNullOrWhitespaceOrMarker(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return true;
            return str.IndexOf(ContentMarker) != -1;
        }

        public struct Content
        {
            public object Object;
            public int? Alignment;
            public string Format;

            public override string ToString()
            {
                string alignmentStr = Alignment.HasValue ? $",{Alignment}" : null;
                string formatStr = Format != null ? $":{Format}" : null;
                return FormattableStringFactory.Create($"{{{0}{alignmentStr}{formatStr}}}", new { Object }).ToString();
            }
        }
    }
}