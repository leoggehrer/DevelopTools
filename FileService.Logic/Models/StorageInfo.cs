using System;
using System.Collections.Generic;
using System.Text;

namespace FileService.Logic.Models
{
    [Serializable]
    public class StorageInfo
    {
        public DateTime? LastSync { get; internal set; }
        public string SourcePath { get; internal set; }
        public string FileSubPath { get; internal set; }
        public DateTime LastWriteTime { get; internal set; }

        public override int GetHashCode()
        {
            return FileSubPath.GetHashCode();
        }
        public override string ToString()
        {
            return FileSubPath;
        }
    }
}
