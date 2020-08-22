using System;
using FileService.Logic.Synchronizer;
using FileService.Logic.Watchers;

namespace FileService.Logic
{
    public static class Factory
    {
        public static Action<string> DefaultLogger { get; set; } = t => System.Diagnostics.Debug.WriteLine(t);
        public static Contracts.IFileService CreateMkTranslator(string sourceWatchPath, string sourceWatchFilter, string targetPath)
        {
            return new MdTranslator(sourceWatchPath, sourceWatchFilter, targetPath, false);
        }
        public static Contracts.IFileService CreateDevCssTranslator(string sourceWatchPath, string sourceWatchFilter,
            string targetPath, string targetExt)
        {
            return CreateDevCssTranslator(sourceWatchPath, sourceWatchFilter, targetPath, targetExt, false);
        }
        public static Contracts.IFileService CreateDevCssTranslator(string sourceWatchPath, string sourceWatchFilter,
            string targetPath, string targetExt, bool includeSubdirectories)
        {
            return new DevCssTranslator(sourceWatchPath, sourceWatchFilter, targetPath, targetExt, includeSubdirectories);
        }
        public static Contracts.IFileService CreateDevHtmlTranslator(string sourceWatchPath, string sourceWatchFilter,
            string targetPath, string targetExt)
        {
            return CreateDevHtmlTranslator(sourceWatchPath, sourceWatchFilter, targetPath, targetExt, false);
        }
        public static Contracts.IFileService CreateDevHtmlTranslator(string sourceWatchPath, string sourceWatchFilter,
            string targetPath, string targetExt, bool includeSubdirectories)
        {
            return new DevHtmlTranslator(sourceWatchPath, sourceWatchFilter, targetPath, targetExt, includeSubdirectories);
        }
        public static Contracts.IFileService CreatePathWatcher(string sourceWatchPath, 
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
        public static Contracts.IFileService CreatePathSynchronizer(string sourceWatchPath,
                                                                    string fileFilter,
                                                                    string targetPath,
                                                                    bool includeSubdirectories = true,
                                                                    string[] excludeSubDirectories = null,
                                                                    string[] excludeFileExtensions = null,
                                                                    int delayInMin = 60)
        {
            return new PathSynchronizer(sourceWatchPath, fileFilter, targetPath, includeSubdirectories, excludeSubDirectories, excludeFileExtensions, delayInMin)
            {
                Logger = DefaultLogger
            };
        }
    }
}
