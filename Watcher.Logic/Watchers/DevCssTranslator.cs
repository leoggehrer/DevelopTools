using DevelopCommon.Extensions;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Watcher.Logic.Extensions;

namespace Watcher.Logic.Watchers
{
    class DevCssTranslator : FileWatcherBase
    {
        public string TargetPath { get; }
        public string TargetExt { get; } = ".css";

        public DevCssTranslator(string sourceWatchPath, string sourceWatchFilter, string targetPath, string targetExt, bool includeSubdirectories)
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
            base.HandleCreatedFile(sender, e);

            HandleFileEvent(sender, e);
        }

        #region Everything is only necessary because of the Windows
        private const int NumberOfRetries = 5;
        private const int DelayOnRetry = 1000;

        private string ReadAllTextFromWindowsFile(string fullPath)
        {
            var result = string.Empty;

            for (int i = 1; i <= NumberOfRetries; ++i)
            {
                try
                {
                    // Do stuff with file
                    result = File.ReadAllText(fullPath, Encoding.UTF8);
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

        protected string ReadAllTextFromFile(string fullPath)
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == true ? ReadAllTextFromWindowsFile(fullPath)
                                                                               : File.ReadAllText(fullPath, Encoding.UTF8);
        }

        protected virtual void HandleFileEvent(object sender, FileSystemEventArgs e)
        {
            if (File.Exists(e.FullPath) == true)
            {
                string[] filterItems = WatchFilter.Split('*');
                string targetName = Path.GetFileNameWithoutExtension(e.Name);
                string devCssText = ReadAllTextFromFile(e.FullPath);

                if (filterItems.Length > 0 && targetName.StartsWith(filterItems[0], StringComparison.CurrentCultureIgnoreCase))
                {
                    targetName = targetName.Substring(filterItems[0].Length);
                }

                string targetFilePath = Path.Combine(TargetPath, $"{targetName}{TargetExt}");
                string cssText = TranslateDevCssToCss(devCssText);

                File.WriteAllText(targetFilePath, cssText, Encoding.UTF8);
            }
        }

        protected string[] DevCssConstTags { get; } =  {
            "$",
            ";",
            "@constants",
            "}"
        };
        protected string[] DevCssBlockTags { get; } =  {
            "@",
            "}"
        };

        protected virtual string TranslateDevCssToCss(string text)
        {
            string result = text;

            if (string.IsNullOrEmpty(text) == false)
            {
                foreach (var tagInfo in result.GetAllTags(DevCssConstTags))
                {
                    result = result.Replace(tagInfo.GetFullText(), string.Empty);
                    if (tagInfo.StartTag.Equals("$"))
                    {
                        var data = tagInfo.GetInnerText().Split(':');

                        if (data.Length == 2)
                        {
                            result = result.Replace($"{data[0]}", $"{data[1]}");
                        }
                    }
                    else if (tagInfo.StartTag.Equals("@constants"))
                    {
                        string[] constants = tagInfo.GetInnerText().Replace("{", string.Empty).Split(';');

                        foreach (var constant in constants)
                        {
                            string[] data = constant.Split(':');

                            if (data.Length == 2)
                            {
                                string name = data[0].Replace(Environment.NewLine, string.Empty).TrimStart();

                                result = result.Replace(name, data[1]);
                            }
                        }

                    }
                }
                foreach (var tagInfo in result.GetAllTags(DevCssBlockTags))
                {
                    result = result.Replace(tagInfo.GetFullText(), string.Empty);
                    if (tagInfo.StartTag.Equals("@"))
                    {
                        foreach (var item in tagInfo.GetFullText().GetAllTags(new string[] { "@", "{" }))
                        {
                            foreach (var subItem in tagInfo.GetFullText().GetAllTags(new string[] { "{", "}" }))
                            {
                                result = result.Replace(item.GetInnerText().Fulltrim(), subItem.GetInnerText());
                            }
                        }
                    }
                }
                result = result.ToLines().ToArray().FormatBlockCode().ToText();
            }
            return result;
        }
    }
}
