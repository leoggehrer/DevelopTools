using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileService.Logic.Watchers
{
    class PathWatcher : FileWatcherBase
    {
        private const int FileOperationDelay = 100;
        private int UpdateDelay { get; }
        private volatile bool updateRunning = false;
        private object lockObject = new object();
        private List<string> changedList = new List<string>();

        private DateTime lastTargetPathSync;
        private DateTime lastWatchPahtSync;

        public string TargetPath { get; }
        public string[] ExcludeSubDirectories { get; }
        public string[] ExcludeFileExtensions { get; }

        public PathWatcher(string watchPath,
                                string watchFilter,
                                string targetPath,
                                bool includeSubdirectories = true,
                                string[] excludeSubDirectories = null,
                                string[] excludeFileExtensions = null,
                                int updateDelayInSec = 60)
            : base(watchPath, watchFilter)
        {
            if (string.IsNullOrWhiteSpace(targetPath))
                throw new ArgumentException(nameof(targetPath));

            if (watchFilter.Equals(targetPath, StringComparison.CurrentCultureIgnoreCase))
                throw new ArgumentException("Watch path can not be equal to the target path!");

            TargetPath = targetPath;
            IncludeSubdirectories = includeSubdirectories;
            ExcludeSubDirectories = excludeSubDirectories ?? new string[0];
            ExcludeFileExtensions = excludeFileExtensions ?? new string[0];

            UpdateDelay = updateDelayInSec * 1000;

            lastTargetPathSync = SyncTargetPathToWatchPath();
            lastWatchPahtSync = SyncWatchPathToTargetPath();

            var updateThread = new Thread(UpdateChangedFiles);

            updateThread.IsBackground = true;
            updateRunning = true;
            updateThread.Start();
        }

        private string GetTargetDirectory(string watchFilePath)
        {
            string watchDirectory = Path.GetDirectoryName(watchFilePath);
            string subPath = GetSubPath(watchDirectory, WatchPath);
            string targetDirectory = string.IsNullOrEmpty(subPath) ? TargetPath : Path.Combine(TargetPath, subPath);

            return targetDirectory;
        }
        private string GetTargetFilePath(string watchFilePath)
        {
            string watchDirectory = Path.GetDirectoryName(watchFilePath);
            string subPath = GetSubPath(watchDirectory, WatchPath);
            string watchFileName = Path.GetFileName(watchFilePath);
            string targetDirectory = string.IsNullOrEmpty(subPath) ? TargetPath : Path.Combine(TargetPath, subPath);
            string targetFilePath = Path.Combine(targetDirectory, watchFileName);

            return targetFilePath;
        }
        private string GetWatchDirectory(string targetFilePath)
        {
            string targetDirectory = Path.GetDirectoryName(targetFilePath);
            string subPath = GetSubPath(targetDirectory, TargetPath);
            string watchDirectory = string.IsNullOrEmpty(subPath) ? WatchPath : Path.Combine(WatchPath, subPath);

            return watchDirectory;
        }
        private string GetWatchFilePath(string targetFilePath)
        {
            string targetDirectory = Path.GetDirectoryName(targetFilePath);
            string subPath = GetSubPath(targetDirectory, TargetPath);
            string targetFileName = Path.GetFileName(targetFilePath);
            string watchDirectory = string.IsNullOrEmpty(subPath) ? WatchPath : Path.Combine(WatchPath, subPath);
            string watchFilePath = Path.Combine(watchDirectory, targetFileName);

            return watchFilePath;
        }
        private DateTime SyncTargetPathToWatchPath()
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
        private DateTime SyncWatchPathToTargetPath()
        {
            var watchFiles = IncludeSubdirectories == true ? Directory.GetFiles(WatchPath, FileFilter, SearchOption.AllDirectories)
                                                           : Directory.GetFiles(WatchPath, FileFilter);
            var syncFiles = watchFiles.Where(f => ExcludeSubDirectories.Any(e => f.ToLower().Contains($"\\{e.ToLower()}\\")) == false);

            foreach (var item in syncFiles)
            {
                SynchronizeWatchFile(item);
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
        private void SynchronizeWatchFile(string filePath)
        {
            string targetFilePath = GetTargetFilePath(filePath);

            if (File.Exists(targetFilePath) == true)
            {
                for (; ; )
                {
                    if (IsFileLocked(filePath) == false && IsFileLocked(targetFilePath) == false)
                    {
                        FileInfo watchFileInfo = new FileInfo(filePath);
                        FileInfo targetFileInfo = new FileInfo(targetFilePath);

                        if (watchFileInfo.LastWriteTime > targetFileInfo.LastWriteTime)
                        {
                            ToLog($"{MethodBase.GetCurrentMethod().Name}: Copy {filePath} -> {targetFilePath}");
                            File.Copy(filePath, targetFilePath, true);
                        }
                        break;
                    }
                    Thread.Sleep(FileOperationDelay);
                }
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
        private void RenameTargetDirectory(string oldWatchPath, string newWatchPath)
        {
            string oldDifPath = GetSubPath(oldWatchPath, WatchPath);
            string newDifPath = GetSubPath(newWatchPath, WatchPath);
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

        protected override void HandleCreatedFile(object sender, FileSystemEventArgs e)
        {
            base.HandleCreatedFile(sender, e);

            if (File.Exists(e.FullPath)
                && IsHandleFile(e.FullPath))
            {
                CreateTargetFile(e.FullPath);
            }
        }
        protected override void HandleChangedFile(object sender, FileSystemEventArgs e)
        {
            base.HandleChangedFile(sender, e);

            lock (lockObject)
            {
                if (changedList.Contains(e.FullPath) == false && IsHandleFile(e.FullPath))
                {
                    changedList.Add(e.FullPath);
                }
            }
        }
        protected override void HandleRenamedFile(object sender, RenamedEventArgs e)
        {
            base.HandleRenamedFile(sender, e);

            if (File.Exists(e.FullPath))
            {
                if (IsHandleFile(e.FullPath) == true)
                {
                    RenameTargetFile(e.OldFullPath, e.FullPath);
                }
            }
            else
            {
                RenameTargetDirectory(e.OldFullPath, e.FullPath);
            }
        }
        protected override void HandleDeletedFile(object sender, FileSystemEventArgs e)
        {
            base.HandleDeletedFile(sender, e);

            if (IsHandleFile(e.FullPath) == true)
            {
                DeleteTargetFile(e.FullPath);
            }
        }

        private void UpdateChangedFiles()
        {
            while (updateRunning)
            {
                List<string> updateFiles = new List<string>();
                List<string> errorFiles = new List<string>();

                lock (lockObject)
                {
                    updateFiles.AddRange(changedList);
                    changedList.Clear();
                }

                foreach (var item in updateFiles)
                {
                    try
                    {
                        if (File.Exists(item)
                            && IsHandleFile(item) == true)
                        {
                            SynchronizeWatchFile(item);
                        }
                    }
                    catch (Exception ex)
                    {
                        errorFiles.Add(item);
                        System.Diagnostics.Debug.WriteLine($"{System.Reflection.MethodBase.GetCurrentMethod().Name}: {ex.Message}");
                    }
                }

                lock(lockObject)
                {
                    foreach (var item in errorFiles)
                    {
                        if (changedList.Contains(item) == false)
                        {
                            changedList.Add(item);
                        }
                    }
                }

                var now = DateTime.Now;
                var dif = now - lastWatchPahtSync;

                if (now.Hour > 0 && now.Hour < 6 && dif.TotalHours > 5)
                {
                    lastWatchPahtSync = SyncWatchPathToTargetPath();
                }
                else
                {
                    Thread.Sleep(UpdateDelay);
                }
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
