using System;

namespace Watcher.Contracts
{
    public interface IFileWatcher
    {
        Action<string> Logger { get; set; }
        bool IncludeSubdirectories { get; }
        string WatchPath { get; }
        string WatchFilter { get; }
        void Start();
        void Stop();
    }
}
