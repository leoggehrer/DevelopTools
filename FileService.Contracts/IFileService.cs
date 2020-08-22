using System;

namespace FileService.Contracts
{
    public interface IFileService
    {
        Action<string> Logger { get; set; }
        bool IncludeSubdirectories { get; }
        string WatchPath { get; }
        string FileFilter { get; }
        void Start();
        void Stop();
    }
}
