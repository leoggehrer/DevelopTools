using DevelopCommon.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileService.Logic.Watchers
{
    internal class MdTranslator : FileWatcherBase
    {
        public string TargetPath { get; }
        public MdTranslator(string sourceWatchPath, string sourceWatchFilter, string targetPath, bool includeSubdirectories)
            : base(sourceWatchPath, sourceWatchFilter)
        {
            if (string.IsNullOrWhiteSpace(targetPath))
                throw new ArgumentException(nameof(targetPath));

            TargetPath = targetPath;
            IncludeSubdirectories = includeSubdirectories;
        }

        protected override void HandleCreatedFile(object sender, FileSystemEventArgs e)
        {
            base.HandleCreatedFile(sender, e);

            HandleFileEvent(sender, e);
        }

        protected override void HandleChangedFile(object sender, FileSystemEventArgs e)
        {
            base.HandleCreatedFile(sender, e);

            HandleFileEvent(sender, e);
        }

        #region Everything is only necessary because of the Windows
        private const int NumberOfRetries = 5;
        private const int DelayOnRetry = 1000;

        private StreamReader CreateStreamReaderFromWindowsFile(string fullPath)
        {
            var result = default(StreamReader);

            for (int i = 1; i <= NumberOfRetries; ++i)
            {
                try
                {
                    // Do stuff with file
                    result = new StreamReader(fullPath);
                    break; // When done we can break loop
                }
                catch when (i <= NumberOfRetries)
                {
                    // You may check error code to filter some exceptions, not every error
                    // can be recovered.
                    Thread.Sleep(DelayOnRetry);
                }
            }
            return result;
        }
        private string ReadAllTextFromWindowsFile(string fullPath, Encoding encoding)
        {
            var result = default(String);

            for (int i = 1; i <= NumberOfRetries; ++i)
            {
                try
                {
                    // Do stuff with file
                    result = File.ReadAllText(fullPath, encoding);
                    break; // When done we can break loop
                }
                catch when (i <= NumberOfRetries)
                {
                    // You may check error code to filter some exceptions, not every error
                    // can be recovered.
                    Thread.Sleep(DelayOnRetry);
                }
            }
            return result;
        }
        #endregion

        protected StreamReader CreateStreamReaderFromFile(string fullPath)
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == true
                ? CreateStreamReaderFromWindowsFile(fullPath)
                : new StreamReader(fullPath);
        }

        protected virtual void HandleFileEvent(object sender, FileSystemEventArgs e)
        {
            if (IsWatchFile(e.FullPath) == true
                && File.Exists(e.FullPath) == true)
            {
                if (replaceFiles.Contains(e.FullPath) == false)
                {
                    ReplaceDocumentTagsAsync(e.FullPath);
                }
            }
        }

        private List<string> replaceFiles = new List<string>();
        private Task ReplaceDocumentTagsAsync(string fullPath)
        {
            Task result;

            result = Task.Run(() =>
            {
                if (replaceFiles.Contains(fullPath) == false)
                {
                    replaceFiles.Add(fullPath);
                    string sourceText = ReadAllTextFromWindowsFile(fullPath, Encoding.Default);
                    string replaceText = ReplaceDocumentTags(sourceText);

                    if (sourceText.Equals(replaceText) == false)
                    {
                        try
                        {
                            File.WriteAllText(fullPath, replaceText, Encoding.Default);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"{MethodBase.GetCurrentMethod().Name}: {ex.Message}");
                        }
                    }
                    replaceFiles.Remove(fullPath);
                }
            });
            return result;
        }
        private static string MdCommentBegin => "[//]: # ";
        private static string MdCommentEnd => "[//]: # ";
        private static string MdEmbeddedBegin => $"{MdCommentBegin}";
        private static string MdEmbeddedEnd => $"{MdCommentEnd}";
        private static string[] EmbeddedTags => new[]
        {
            MdEmbeddedBegin,
            MdEmbeddedEnd
        };
        class TagParameters
        {
            public string Type { get; set; }
            public string Begin { get; set; }
            public string File { get; set; }
            public string StartTag { get; set; }
            public string EndTag { get; set; }
            public string End { get; set; }
        }

        private string ReplaceDocumentTags(string text)
        {
            int textStartPos = 0;
            StringBuilder result = new StringBuilder();

            foreach (var tag in text.GetAllTags(EmbeddedTags))
            {
                var tagLines = tag.GetFullLines();
                var startTagParams = tagLines.First().Partialstring("(", ")");
                var endTagParams = tagLines.Last().Partialstring("(", ")");
                var startParams = System.Text.Json.JsonSerializer.Deserialize<TagParameters>(startTagParams);
                var endParams = System.Text.Json.JsonSerializer.Deserialize<TagParameters>(endTagParams);

                result.Append(text.Partialstring(textStartPos, tag.StartTagIndex - 1));
                if (tagLines.Any())
                {
                    result.AppendLine(tagLines.First());
                }
                if (startParams.Begin.HasContent())
                {
                    result.AppendLine(startParams.Begin);
                }

                try
                {
                    if (startParams.Type.HasContent()
                        && startParams.Type.Equals("FileRef", StringComparison.CurrentCultureIgnoreCase)
                        && startParams.File.HasContent())
                    {
                        var files = Directory.GetFiles(WatchPath, "*.*", SearchOption.AllDirectories)
                                             .Select(f => f.ToLower().Replace(@"\", "/"));
                        var file = files.FirstOrDefault(f => f.EndsWith(startParams.File.ToLower()));

                        if (file != null)
                        {
                            string fileContent = ReadAllTextFromWindowsFile(file, Encoding.Default);
                            var embeddedTag = fileContent.GetAllTags(startParams.StartTag, startParams.EndTag).FirstOrDefault();

                            if (embeddedTag != null)
                            {
                                result.Append(embeddedTag.GetInnerText());
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{MethodBase.GetCurrentMethod().Name}: {ex.Message}");
                }
                textStartPos = tag.EndTagIndex;

                if (endParams.End.HasContent())
                {
                    result.AppendLine(endParams.End);
                }
                if (tagLines.Any())
                {
                    result.AppendLine(tagLines.Last());
                    textStartPos += tagLines.Last().Length;
                }
            }
            if (result.Length > 0)
            {
                if (textStartPos < text.Length - 1)
                {
                    result.Append(text.Partialstring(textStartPos, text.Length - 1));
                }
            }
            return result.ToString();
        }

        protected bool IsWatchFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            return Path.GetExtension(filePath).Equals(FileFilter.Replace("*", string.Empty), StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
