using DevelopCommon.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FileService.Logic.Extensions
{
    internal static class FormatterExtensions
    {
        public static string BlockStartPrefix { get; set; } = " ";
        public static char BlockStart { get; set; } = '{';
        public static char BlockEnd { get; set; } = '}';
        public static string BlockEndPrefix { get; set; } = Environment.NewLine;
        public static string CommentStart { get; set; } = "/*";
        public static string CommentEnd { get; set; } = "*/";

        #region CodeFormatter
        private static bool HasFullCodeBlock(this string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            int codeBegin = 0;
            int codeEnd = 0;

            foreach (var chr in text)
            {
                if (chr == BlockStart)
                    codeBegin++;
                else if (chr == BlockEnd)
                    codeEnd++;
            }
            return codeBegin > 0 && codeBegin == codeEnd;
        }
        private static string TrimBlockLine(this string text)
        {
            if (String.IsNullOrEmpty(text) == false)
            {
                Regex trimmer = new Regex(@"\s\s+");

                text = text.Replace("\t", String.Empty);
                text = text.Replace(System.Environment.NewLine, String.Empty);

                text = text.Trim();
                text = trimmer.Replace(text, " ");
            }
            return text;
        }
        private static bool GetBlockPositions(string text, ref int start, ref int end)
        {
            int cornerBraket = 0;
            int blockBegin = 0;
            int blockEnd = 0;

            start = end = -1;
            for (int idx = 0; idx >= 0 && idx < text.Length && (start == -1 || end == -1); idx++)
            {
                char chr = text[idx];

                if (chr == '[')
                    cornerBraket++;
                else if (chr == ']')
                    cornerBraket++;
                else if (chr == BlockStart && cornerBraket % 2 == 0)
                {
                    blockBegin++;
                    if (blockBegin == 1)
                        start = idx;
                }
                else if (chr == BlockEnd && cornerBraket % 2 == 0)
                {
                    blockEnd++;
                    if (blockEnd == blockBegin)
                        end = idx;
                }
            }
            return blockBegin > 0 && blockEnd > 0 && blockBegin == blockEnd;
        }
        private static string[] SplitCodeAssignments(this string line)
        {
            if (line == null)
                throw new ArgumentNullException(nameof(line));

            int startIdx = -1;
            int endIdx = 0;
            List<string> lines = new List<string>();

            while ((endIdx = line.IndexOf(';', startIdx + 1)) >= 0)
            {
                lines.Add(line.Partialstring(startIdx + 1, endIdx).TrimBlockLine());
                startIdx = endIdx;
            }
            string endPartial = line.Partialstring(startIdx + 1, line.Length - 1).TrimBlockLine();

            if (endPartial.Length > 0)
            {
                lines.Add(endPartial);
            }
            return lines.ToArray();
        }
        private static string[] SplitLine(this string line, string left, string right)
        {
            if (line == null)
                throw new ArgumentNullException(nameof(line));

            int lastIdx = -1;
            int startIdx = -1;
            int endIdx = 0;
            List<string> lines = new List<string>();

            while ((startIdx = line.IndexOf(left, startIdx + 1, StringComparison.Ordinal)) >= 0
                   && (endIdx = line.IndexOf(right, startIdx + 1, StringComparison.Ordinal)) > startIdx
                   && endIdx - startIdx > 1)
            {
                lines.Add(line.Partialstring(startIdx, endIdx).TrimBlockLine());
                lastIdx = startIdx = endIdx;
            }
            string endPartial = line.Partialstring(lastIdx + 1, line.Length - 1).TrimBlockLine();

            if (endPartial.Length > 0)
            {
                lines.Add(endPartial);
            }
            return lines.ToArray();
        }
        private static string[] SplitBlockLine(this string line)
        {
            if (line == null)
                throw new ArgumentNullException(nameof(line));

            var lines = new List<string>();

            lines.AddRange(line.SplitCodeAssignments());

            List<string> result = new List<string>();

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Length > 0)
                {
                    int idx;

                    if ((idx = lines[i].IndexOf(CommentStart, StringComparison.Ordinal)) >= 0
                        &&
                        (idx = lines[i].IndexOf(CommentEnd, idx + 1, StringComparison.Ordinal)) >= 0)
                    {
                        result.Add(lines[i]);
                    }
                    else if ((idx = lines[i].IndexOf(CommentStart, StringComparison.Ordinal)) >= 0)
                    {
                        if (idx > 1)
                        {
                            string partLine = lines[i].Partialstring(0, idx - 1).TrimBlockLine();

                            if (partLine.Length > 0)
                                result.Add(partLine);
                        }

                        result.Add(CommentStart);
                        if (idx + 2 < lines[i].Length - 1)
                        {
                            string partLine = lines[i].Partialstring(idx + 2, lines[i].Length - 1).TrimBlockLine();

                            if (partLine.Length > 0)
                                result.Add(partLine);
                        }
                    }
                    else if ((idx = lines[i].IndexOf(CommentEnd, StringComparison.Ordinal)) >= 0)
                    {
                        if (idx > 1)
                        {
                            string partLine = lines[i].Partialstring(0, idx - 1).TrimBlockLine();

                            if (partLine.Length > 0)
                                result.Add(partLine);
                        }

                        result.Add(CommentEnd);
                        if (idx + 2 < lines[i].Length - 1)
                        {
                            string partLine = lines[i].Partialstring(idx + 2, lines[i].Length - 1).TrimBlockLine();

                            if (partLine.Length > 0)
                                result.Add(partLine);
                        }
                    }
                    else
                    {
                        result.AddRange(lines[i].SplitLine("[", "]"));
                    }
                }
            }
            return result.ToArray();
        }
        public static string[] FormatBlockCode(this string[] lines)
        {
            return lines.FormatBlockCode(0);
        }
        public static string[] FormatBlockCode(this string[] lines, int indent)
        {
            if (lines == null)
                throw new ArgumentNullException(nameof(lines));

            string text = lines.ToText();
            List<string> result = new List<string>();

            if (text.HasFullCodeBlock() == true)
            {
                text.FormatCodeBlock(indent, result);
            }
            else
            {
                result.AddRange(lines);
            }
            return result.ToArray();
        }
        private static void FormatCodeBlock(this string text, int indent, List<string> lines)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            if (lines == null)
                throw new ArgumentNullException(nameof(lines));

            int beginPos = 0;
            int endPos = 0;

            void AddCodeLines(string txt, int idt, List<string> list)
            {
                string[] items = txt.SplitBlockLine();

                list.AddRange(items.Where(l => l.Length > 0)
                    .Select(l => l.SetIndent(idt))
                    .ToArray());
            }

            if (GetBlockPositions(text, ref beginPos, ref endPos) == true)
            {
                AddCodeLines(text.Partialstring(0, beginPos - 1), indent, lines);

                if (BlockStartPrefix.Equals(Environment.NewLine))
                {
                    lines.Add(BlockStart.ToString().SetIndent(indent));
                }
                else
                {
                    lines[lines.Count - 1] = $"{lines[lines.Count - 1]}{BlockStartPrefix}{BlockStart}";
                }

                text.Partialstring(beginPos + 1, endPos - 1).FormatCodeBlock(indent + 1, lines);

                if (BlockEndPrefix.Equals(Environment.NewLine))
                {
                    lines.Add(BlockEnd.ToString().SetIndent(indent));
                }
                else
                {
                    lines[lines.Count - 1] = $"{lines[lines.Count - 1]}{BlockEndPrefix}{BlockEnd}";
                }

                text.Partialstring(endPos + 1, text.Length - 1).FormatCodeBlock(indent, lines);
            }
            else
            {
                AddCodeLines(text, indent, lines);
            }
        }
        #endregion
    }
}
