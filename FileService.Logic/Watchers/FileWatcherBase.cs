using System;
using System.IO;

namespace FileService.Logic.Watchers
{
    abstract class FileWatcherBase : Contracts.IFileService
    {
        private FileSystemWatcher FileWatcher { get; set; } = null;

        public bool IncludeSubdirectories { get; protected set; }
        public string WatchPath { get; private set; }
        public string FileFilter { get; private set; }
        public Action<string> Logger { get; set; } = t => System.Diagnostics.Debug.WriteLine(t);

        protected FileWatcherBase(string watchPath, string watchFilter)
        {
            if (string.IsNullOrWhiteSpace(watchPath))
                throw new ArgumentException(nameof(watchPath));

            if (string.IsNullOrWhiteSpace(watchFilter))
                throw new ArgumentException(nameof(watchFilter));

            WatchPath = watchPath;
            FileFilter = watchFilter;
        }
        protected virtual void InitFileWatcher(string sourcePath, string filter)
        {
            if (FileWatcher == null)
            {
                FileWatcher = new FileSystemWatcher(sourcePath, filter)
                {
                    IncludeSubdirectories = IncludeSubdirectories
                };

                FileWatcher.Created += HandleCreatedFile;
                FileWatcher.Changed += HandleChangedFile;
                FileWatcher.Deleted += HandleDeletedFile;
                FileWatcher.Renamed += HandleRenamedFile;
            }
        }

        public void Start()
        {
            if (FileWatcher == null)
            {
                InitFileWatcher(WatchPath, FileFilter);
			}
            FileWatcher.EnableRaisingEvents = true;
        }
        public void Stop()
        {
            if (FileWatcher != null)
            {
                FileWatcher.EnableRaisingEvents = false;
            }
        }

        protected virtual void HandleCreatedFile(object sender, FileSystemEventArgs e)
        {
        }
        protected virtual void HandleChangedFile(object sender, FileSystemEventArgs e)
        {
        }
        protected virtual void HandleDeletedFile(object sender, FileSystemEventArgs e)
        {
        }
        protected virtual void HandleRenamedFile(object sender, RenamedEventArgs e)
        {
        }
    }
}
