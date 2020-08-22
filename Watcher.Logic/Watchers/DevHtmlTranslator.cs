using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Watcher.Logic.Watchers
{
    internal class DevHtmlTranslator : FileWatcherBase
    {
        private const int MaxLevel = 20;
        private const string IncludeTagName = "include";
        private List<string> VisitedItems { get; set; } = new List<string>();
        private Dictionary<string, List<string>> ReferenceItems { get; set; } = new Dictionary<string, List<string>>();

        public string TargetPath { get; }
        public string TargetExt { get; } = ".html";
        public string WatchFileNamePrefix { get; set; } = "dev_";

        public DevHtmlTranslator(string sourceWatchPath, string sourceWatchFilter, string targetPath, string targetExt, bool includeSubdirectories)
            : base(sourceWatchPath, sourceWatchFilter)
        {
            if (string.IsNullOrWhiteSpace(targetPath))
                throw new ArgumentException(nameof(targetPath));

            if (string.IsNullOrWhiteSpace(TargetExt))
                throw new ArgumentException(nameof(targetExt));

            TargetPath = targetPath;
            TargetExt = targetExt;
            IncludeSubdirectories = includeSubdirectories;
        }

        protected override void HandleCreatedFile(object sender, FileSystemEventArgs e)
        {
            base.HandleCreatedFile(sender, e);

            HandleFileEvent(sender, e);
        }
        protected override void HandleChangedFile(object sender, FileSystemEventArgs e)
        {
            base.HandleChangedFile(sender, e);

            HandleFileEvent(sender, e);
        }
        protected virtual void HandleFileEvent(object sender, FileSystemEventArgs e)
        {
            if (IsWatchFile(e.FullPath) == true
                && File.Exists(e.FullPath) == true)
            {
                TranslateDevHtmlDocument(e.FullPath, GetTragetFilePath(e.FullPath));
            }
            else if (ReferenceItems.ContainsKey(e.FullPath) == true)
            {
                foreach (var item in ReferenceItems[e.FullPath])
                {
                    TranslateDevHtmlDocument(item, GetTragetFilePath(item));
                }
            }
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
        #endregion

        protected StreamReader CreateStreamReaderFromFile(string fullPath)
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == true
                ? CreateStreamReaderFromWindowsFile(fullPath)
                : new StreamReader(fullPath);
        }

        protected virtual void TranslateDevHtmlDocument(string sourceFilePath, string targetFilePath)
        {
            HtmlDocument htmlDoc = new HtmlDocument();

            VisitedItems.Clear();
            try
            {
                using (var srcTxtReader = CreateStreamReaderFromFile(sourceFilePath))
                using (var tarTxtWriter = new StreamWriter(targetFilePath))
                {
                    List<HtmlNode> scriptNodes = new List<HtmlNode>();

                    htmlDoc.Load(srcTxtReader);
                    htmlDoc.DocumentNode.SelectNodes("//script")?.ToList()
                                        .ForEach(n =>
                                        {
                                            n.Remove();
                                            scriptNodes.Add(n);
                                        });

                    foreach (var node in htmlDoc.DocumentNode.ChildNodes)
                    {
                        scriptNodes.AddRange(TranslateHtmlNodes(node.ChildNodes, 0));
                    }

                    var body = htmlDoc.DocumentNode.SelectNodes("//body")
                                      .SingleOrDefault();

                    scriptNodes.ForEach(n =>
                    {
                        body?.ChildNodes.Add(HtmlNode.CreateNode(Environment.NewLine));
                        body?.ChildNodes.Add(n);
                    });

                    FormatHtmlNodes(htmlDoc.DocumentNode.ChildNodes, 0);
                    htmlDoc.OptionFixNestedTags = true;
                    htmlDoc.Save(tarTxtWriter);
                }
                foreach (var includeFilePath in VisitedItems)
                {
                    if (ReferenceItems.ContainsKey(includeFilePath) == true)
                    {
                        if (ReferenceItems[includeFilePath].Contains(sourceFilePath) == false)
                        {
                            ReferenceItems[includeFilePath].Add(sourceFilePath);
                        }
                    }
                    else
                    {
                        ReferenceItems.Add(includeFilePath, new List<string>());
                        ReferenceItems[includeFilePath].Add(sourceFilePath);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }
        protected virtual HtmlNode[] TranslateHtmlNodes(HtmlNodeCollection nodes, int level)
        {
            List<HtmlNode> scriptNodes = new List<HtmlNode>();
            List<HtmlNode> includeNodes = new List<HtmlNode>();

            if (nodes == null || level > MaxLevel)
                return scriptNodes.ToArray();

            foreach (var node in nodes)
            {
                if (node.Name.ToLower().Contains(IncludeTagName) == true)
                {
                    includeNodes.Add(node);
                }
                else
                {
                    scriptNodes.AddRange(TranslateHtmlNodes(node.ChildNodes, level + 1));
                }
            }

            foreach (var node in includeNodes)
            {
                HtmlDocument htmlDoc = new HtmlDocument();

                try
                {
                    string includeFilePath = Path.Combine(WatchPath, node.InnerText);

                    VisitedItems.Add(includeFilePath);

                    using (var srcFs = new FileStream(includeFilePath, FileMode.Open, FileAccess.Read))
                    using (var srcTxtReader = new StreamReader(srcFs))
                    {
                        var content = srcTxtReader.ReadToEnd();

                        foreach (var attr in node.Attributes)
                        {
                            content = content.Replace(attr.Name, attr.Value);
                        }
                        htmlDoc.Load(new StringReader(content));
                        htmlDoc.DocumentNode.SelectNodes("//script")?.ToList()
                                            .ForEach(n =>
                                            {
                                                n.Remove();
                                                scriptNodes.Add(n);
                                            });

                        foreach (var subNode in htmlDoc.DocumentNode.ChildNodes)
                        {
                            scriptNodes.AddRange(TranslateHtmlNodes(subNode.ChildNodes, level + 1));
                        }
                        node.ParentNode.ReplaceChild(htmlDoc.DocumentNode, node);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }
            return scriptNodes.ToArray();
        }

        protected virtual void FormatHtmlNodes(HtmlNodeCollection nodes, int level)
        {
            if (nodes == null || level > MaxLevel)
                return;

            bool IsWhiteSpaceNode(HtmlNode n) => n?.InnerHtml != null && string.IsNullOrWhiteSpace(n.InnerHtml);

            foreach (var node in nodes)
            {
                if (node.NodeType == HtmlNodeType.Text
                    && IsWhiteSpaceNode(node) == true)
                {
                    node.InnerHtml = Environment.NewLine;
                    node.InnerHtml += new string(' ', level);
                }
                FormatHtmlNodes(node.ChildNodes, level + 1);
            }
        }

        #region Helpers
        protected bool IsWatchFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            string fileName = Path.GetFileNameWithoutExtension(filePath);

            return fileName.StartsWith(WatchFileNamePrefix, StringComparison.CurrentCultureIgnoreCase);
        }
        protected string GetTragetFilePath(string filePath)
        {
            string targetName = Path.GetFileNameWithoutExtension(filePath);

            targetName = targetName.Substring(WatchFileNamePrefix.Length);
            return Path.Combine(TargetPath, $"{targetName}{TargetExt}");
        }
        protected bool HasCycle(string includeFilePath)
        {
            bool result = false;

            if (VisitedItems.Any(i => i.Equals(includeFilePath, StringComparison.CurrentCultureIgnoreCase)))
            {
                int visitedIdx = VisitedItems.Count - 1;

                List<string> periodItems = new List<string> { includeFilePath };

                while (visitedIdx >= 0 && VisitedItems[visitedIdx].Equals(includeFilePath) == false)
                {
                    periodItems.Insert(0, VisitedItems[visitedIdx--]);
                }
                result = ContainsSubsequence(VisitedItems, periodItems);
            }
            return result;
        }
        protected bool ContainsSubsequence<T>(List<T> sequence, List<T> subsequence)
        {
            return Enumerable.Range(0, sequence.Count - subsequence.Count + 1)
                             .Any(n => sequence.Skip(n)
                             .Take(subsequence.Count)
                             .SequenceEqual(subsequence));
        }
        #endregion
    }
}
