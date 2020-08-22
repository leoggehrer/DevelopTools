using System;
using Watcher.Logic.Watchers;

namespace Watcher.Logic
{
    public static class WatcherFactory
    {
        public static Action<string> DefaultLogger { get; set; } = t => System.Diagnostics.Debug.WriteLine(t);
        public static Contracts.IFileWatcher CreateMkTranslator(string sourceWatchPath, string sourceWatchFilter, string targetPath)
        {
            return new MdTranslator(sourceWatchPath, sourceWatchFilter, targetPath, false);
        }
        public static Contracts.IFileWatcher CreateDevCssTranslator(string sourceWatchPath, string sourceWatchFilter,
            string targetPath, string targetExt)
        {
            return CreateDevCssTranslator(sourceWatchPath, sourceWatchFilter, targetPath, targetExt, false);
        }
        public static Contracts.IFileWatcher CreateDevCssTranslator(string sourceWatchPath, string sourceWatchFilter,
            string targetPath, string targetExt, bool includeSubdirectories)
        {
            return new DevCssTranslator(sourceWatchPath, sourceWatchFilter, targetPath, targetExt, includeSubdirectories);
        }
        public static Contracts.IFileWatcher CreateDevHtmlTranslator(string sourceWatchPath, string sourceWatchFilter,
            string targetPath, string targetExt)
        {
            return CreateDevHtmlTranslator(sourceWatchPath, sourceWatchFilter, targetPath, targetExt, false);
        }
        public static Contracts.IFileWatcher CreateDevHtmlTranslator(string sourceWatchPath, string sourceWatchFilter,
            string targetPath, string targetExt, bool includeSubdirectories)
        {
            return new DevHtmlTranslator(sourceWatchPath, sourceWatchFilter, targetPath, targetExt, includeSubdirectories);
        }
        public static Contracts.IFileWatcher CreatePathWatcher(string sourceWatchPath, 
                                                               string sourceWatchFilter, 
                                                               string targetPath,
                                                               bool includeSubdirectories = true, 
                                                               string[] excludeSubDirectories = null, 
                                                               string[] excludeFileExtensions = null)
        {
            return new PathWatcher(sourceWatchPath, sourceWatchFilter, targetPath, includeSubdirectories, excludeSubDirectories, excludeFileExtensions)
            {
                Logger = DefaultLogger
            };
        }
    }
}
