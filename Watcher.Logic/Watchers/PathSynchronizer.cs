using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Watcher.Logic.Watchers
{
    class PathSynchronizer
    {
        private const int FileOperationDelay = 100;
        private int SynchronizeDelay { get; }
        private volatile bool synchronizeRunning = false;
        private object lockObject = new object();
        private List<string> changedList = new List<string>();

        private DateTime lastTargetPathSync;
        private DateTime lastWatchPahtSync;

        public Action<string> Logger { get; set; } = t => System.Diagnostics.Debug.WriteLine(t);
        public string FileFilter { get; }
        public string SourcePath { get; }
        public string TargetPath { get; }
        public bool IncludeSubdirectories { get; private set; }
        public string[] ExcludeSubDirectories { get; }
        public string[] ExcludeFileExtensions { get; }

        public PathSynchronizer(string sourcePath,
                                string fileFilter,
                                string targetPath,
                                bool includeSubdirectories = true,
                                string[] excludeSubDirectories = null,
                                string[] excludeFileExtensions = null,
                                int sysnchronizeDelayInMin = 60)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
                throw new ArgumentException(nameof(sourcePath));

            if (string.IsNullOrWhiteSpace(fileFilter))
                throw new ArgumentException(nameof(fileFilter));

            if (string.IsNullOrWhiteSpace(targetPath))
                throw new ArgumentException(nameof(targetPath));

            FileFilter = fileFilter;
            SourcePath = sourcePath;
            TargetPath = targetPath;
            IncludeSubdirectories = includeSubdirectories;
            ExcludeSubDirectories = excludeSubDirectories ?? new string[0];
            ExcludeFileExtensions = excludeFileExtensions ?? new string[0];

            SynchronizeDelay = sysnchronizeDelayInMin * 60000;

            lastTargetPathSync = SyncTargetPathToSourcePath();
            lastWatchPahtSync = SyncSourcePathToTargetPath();

            var updateThread = new Thread(SynchronizePath);

            updateThread.IsBackground = true;
            synchronizeRunning = true;
            updateThread.Start();
        }

        private string GetTargetDirectory(string watchFilePath)
        {
            string watchDirectory = Path.GetDirectoryName(watchFilePath);
            string subPath = GetSubPath(watchDirectory, SourcePath);
            string targetDirectory = string.IsNullOrEmpty(subPath) ? TargetPath : Path.Combine(TargetPath, subPath);

            return targetDirectory;
        }
        private string GetTargetFilePath(string watchFilePath)
        {
            string watchDirectory = Path.GetDirectoryName(watchFilePath);
            string subPath = GetSubPath(watchDirectory, SourcePath);
            string watchFileName = Path.GetFileName(watchFilePath);
            string targetDirectory = string.IsNullOrEmpty(subPath) ? TargetPath : Path.Combine(TargetPath, subPath);
            string targetFilePath = Path.Combine(targetDirectory, watchFileName);

            return targetFilePath;
        }
        private string GetWatchDirectory(string targetFilePath)
        {
            string targetDirectory = Path.GetDirectoryName(targetFilePath);
            string subPath = GetSubPath(targetDirectory, TargetPath);
            string watchDirectory = string.IsNullOrEmpty(subPath) ? SourcePath : Path.Combine(SourcePath, subPath);

            return watchDirectory;
        }
        private string GetWatchFilePath(string targetFilePath)
        {
            string targetDirectory = Path.GetDirectoryName(targetFilePath);
            string subPath = GetSubPath(targetDirectory, TargetPath);
            string targetFileName = Path.GetFileName(targetFilePath);
            string watchDirectory = string.IsNullOrEmpty(subPath) ? SourcePath : Path.Combine(SourcePath, subPath);
            string watchFilePath = Path.Combine(watchDirectory, targetFileName);

            return watchFilePath;
        }
        protected DateTime SyncTargetPathToSourcePath()
        {
            var targetFiles = IncludeSubdirectories == true ? Directory.GetFiles(TargetPath, FileFilter, SearchOption.AllDirectories)
                                                            : Directory.GetFiles(TargetPath, FileFilter);
            var syncFiles = targetFiles.Where(f => ExcludeSubDirectories.Any(e => f.ToLower().Contains($"\\{e.ToLower()}\\")) == false);

            foreach (var item in syncFiles)
            {
                SynchronizeTargetFile(item);
            }
            return DateTime.Now;
        }
        protected DateTime SyncSourcePathToTargetPath()
        {
            var sourceFiles = IncludeSubdirectories ? Directory.GetFiles(SourcePath, FileFilter, SearchOption.AllDirectories)
                                                    : Directory.GetFiles(SourcePath, FileFilter);
            var syncFiles = sourceFiles.Where(f => ExcludeSubDirectories.Any(e => f.ToLower().Contains($"\\{e.ToLower()}\\")) == false);

            foreach (var item in syncFiles)
            {
                SynchronizeSourceFile(item);
            }
            return DateTime.Now;
        }

        private void CreateFile(string sourceFilePath, string targetFilePath)
        {
            string targetDirectory = Path.GetDirectoryName(targetFilePath);

            if (Directory.Exists(targetDirectory) == false)
            {
                Directory.CreateDirectory(targetDirectory);
            }
            if (Directory.Exists(targetDirectory) == true)
            {
                CopyFile(sourceFilePath, targetFilePath);
            }
        }
        private void CreateTargetFile(string watchFilePath)
        {
            string targetDirectory = GetTargetDirectory(watchFilePath);
            string targetFilePath = GetTargetFilePath(watchFilePath);

            if (Directory.Exists(targetDirectory) == false)
            {
                Directory.CreateDirectory(targetDirectory);
            }
            if (Directory.Exists(targetDirectory) == true)
            {
                CopyFile(watchFilePath, targetFilePath);
            }
        }
        private void CopyFile(string sourceFilePath, string targetFilePath)
        {
            bool tryCopy = true;

            while (tryCopy)
            {
                var islocked = IsFileLocked(sourceFilePath);

                if (islocked == null)
                {
                    tryCopy = false;
                }
                else if (islocked == false)
                {
                    ToLog($"{MethodBase.GetCurrentMethod().Name}: Create {sourceFilePath} -> {targetFilePath}");
                    File.Copy(sourceFilePath, targetFilePath, true);
                    tryCopy = false;
                }
                else
                {
                    Thread.Sleep(FileOperationDelay);
                }
            }
        }
        private void OverrideFileAsync(string sourceFilePath, string targetFilePath)
        {
            Task.Run(() =>
            {
                bool tryCopy = true;

                while (tryCopy)
                {
                    var islocked = IsFileLocked(sourceFilePath);

                    if (islocked == null)
                    {
                        tryCopy = false;
                    }
                    else if (islocked == false && IsFileLocked(targetFilePath) != true)
                    {
                        FileInfo sourceFileInfo = new FileInfo(sourceFilePath);
                        FileInfo targetFileInfo = new FileInfo(targetFilePath);

                        if (sourceFileInfo.LastWriteTime > targetFileInfo.LastWriteTime)
                        {
                            ToLog($"{MethodBase.GetCurrentMethod().Name}: Copy {sourceFilePath} -> {targetFilePath}");
                            File.Copy(sourceFilePath, targetFilePath, true);
                            tryCopy = false;
                        }
                    }
                    else
                    {
                        Thread.Sleep(FileOperationDelay);
                    }
                }
            });
        }

        private void SynchronizeTargetFile(string filePath)
        {
            string watchFilePath = GetWatchFilePath(filePath);

            if (File.Exists(watchFilePath) == true)
            {
                for (; ; )
                {
                    if (IsFileLocked(filePath) == false
                        && IsFileLocked(watchFilePath) == false)
                    {
                        FileInfo watchFileInfo = new FileInfo(watchFilePath);
                        FileInfo targetFileInfo = new FileInfo(filePath);

                        if (targetFileInfo.LastWriteTime > watchFileInfo.LastWriteTime)
                        {
                            ToLog($"{MethodBase.GetCurrentMethod().Name}: Copy {filePath} -> {watchFilePath}");
                            File.Copy(filePath, watchFilePath, true);
                        }
                        break;
                    }
                    Thread.Sleep(FileOperationDelay);
                }
            }
            else
            {
                CreateFile(filePath, watchFilePath);
            }
        }
        private void SynchronizeSourceFile(string filePath)
        {
            string targetFilePath = GetTargetFilePath(filePath);

            if (File.Exists(targetFilePath) == true)
            {
                OverrideFileAsync(filePath, targetFilePath);
            }
            else
            {
                CreateFile(filePath, targetFilePath);
            }
        }
        private void RenameTargetFile(string oldWatchFilePath, string newWatchFilePath)
        {
            string oldTargetFilePath = GetTargetFilePath(oldWatchFilePath);
            string newTargetFilePath = GetTargetFilePath(newWatchFilePath);

            if (File.Exists(oldTargetFilePath) == true)
            {
                for (; ; )
                {
                    if (IsFileLocked(oldTargetFilePath) == false)
                    {
                        ToLog($"{MethodBase.GetCurrentMethod().Name}: Rename {oldTargetFilePath} -> {newTargetFilePath}");
                        File.Move(oldTargetFilePath, newTargetFilePath);
                        break;
                    }
                    Thread.Sleep(FileOperationDelay);
                }
            }
        }
        private void RenameTargetDirectory(string oldSourcePath, string newSourcePath)
        {
            string oldDifPath = GetSubPath(oldSourcePath, SourcePath);
            string newDifPath = GetSubPath(newSourcePath, SourcePath);
            string oldTargetPath = Path.Combine(TargetPath, oldDifPath);
            string newTargetPath = Path.Combine(TargetPath, newDifPath);

            if (Directory.Exists(oldTargetPath) == true)
            {
                ToLog($"{MethodBase.GetCurrentMethod().Name}: Copy {oldTargetPath} -> {newTargetPath}");
                Directory.Move(oldTargetPath, newTargetPath);
            }
        }
        private void DeleteTargetFile(string watchItem)
        {
            string targetItem = GetTargetFilePath(watchItem);

            if (File.Exists(targetItem) == true)
            {
                for (; ; )
                {
                    if (IsFileLocked(targetItem) == false)
                    {
                        ToLog($"{MethodBase.GetCurrentMethod().Name}: Delete {targetItem}");
                        File.Delete(targetItem);
                        break;
                    }
                    Thread.Sleep(FileOperationDelay);
                }
            }
            else if (Directory.Exists(targetItem))
            {
                ToLog($"{MethodBase.GetCurrentMethod().Name}: Delete {targetItem}");
                Directory.Delete(targetItem, true);
            }
        }

        private void SynchronizePath()
        {
            while (synchronizeRunning)
            {
                lastWatchPahtSync = SyncSourcePathToTargetPath();
                Thread.Sleep(SynchronizeDelay);
            }
        }

        private bool IsHandleFile(string filePath)
        {
            string ext = Path.GetExtension(filePath);

            return File.Exists(filePath)
                   && ExcludeSubDirectories.Any(e => filePath.ToLower().Contains($"\\{e.ToLower()}")) == false
                   && ExcludeFileExtensions.Any(i => i.Equals(ext, StringComparison.CurrentCultureIgnoreCase)) == false;
        }
        private static bool? IsFileLocked(string filePath)
        {
            bool? result = null;

            if (File.Exists(filePath))
            {
                try
                {
                    using Stream stream = new FileStream(filePath, FileMode.Open);
                    result = false;
                }
                catch (FileNotFoundException)
                {
                    result = null;
                }
                catch (Exception)
                {
                    result = true;
                }
            }
            return result;
        }
        private static string GetSubPath(string fullPath, string partialPath)
        {
            if (string.IsNullOrEmpty(fullPath) == true)
                throw new ArgumentNullException(nameof(fullPath));

            if (string.IsNullOrEmpty(partialPath) == true)
                throw new ArgumentNullException(nameof(partialPath));

            int i = 0;
            StringBuilder sb = new StringBuilder();

            while (i < fullPath.Length && i < partialPath.Length
                   && char.ToLower(fullPath[i]) == char.ToLower(partialPath[i]))
            {
                i++;
            }
            while (i < fullPath.Length && fullPath[i] == '\\')
            {
                i++;
            }
            while (i < fullPath.Length && fullPath[i] == '/')
            {
                i++;
            }
            while (i < fullPath.Length)
            {
                sb.Append(fullPath[i++]);
            }
            return sb.ToString();
        }

        private void ToLog(string text)
        {
            if (Logger != null)
            {
                Task.Run(() => Logger(text));
            }
        }
        #region Helpers
        public static string[] CreateExcludeSubDirecties(params string[] subDirectories)
        {
            List<string> result = new List<string>();

            foreach (var item in subDirectories)
            {
                result.Add($"//{item}");
                result.Add($"\\{item}");
                result.Add($"//{item}//");
                result.Add($"\\{item}\\");
            }
            return result.ToArray();
        }
        #endregion
    }
}
