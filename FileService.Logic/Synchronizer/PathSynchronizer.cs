using FileService.Logic.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FileService.Logic.Synchronizer
{
    class PathSynchronizer : Contracts.IFileService
    {
        private enum SynchronizeAction
        {
            None,
            CopyFirst,
            CopySecond,
            DeleteFirst,
            DeleteSecond
        }

        private const int FileOperationDelay = 100;
        private int SynchronizeDelay { get; }
        private volatile bool synchronizeRunning = false;

        public Action<string> Logger { get; set; } = t => System.Diagnostics.Debug.WriteLine(t);
        public string FirstPath => Data.FirstPath;
        public string FileFilter => Data.FileFilter;
        public string SecondPath => Data.SecondPath;
        public bool IncludeSubdirectories => Data.IncludeSubdirectories;
        public IEnumerable<string> ExcludeSubDirectories => Data.ExcludeSubDirectories;
        public IEnumerable<string> ExcludeFileExtensions => Data.ExcludeFileExtensions;

        private SynchronizerData Data { get; set; }
        public string WatchPath => FirstPath;
        private string StorageInfoFolder = ".ps";
        private string StorageInfoFileName = "_storage_infos_.ser";

        public PathSynchronizer(string firstPath,
                                string fileFilter,
                                string secondPath,
                                bool includeSubdirectories = true,
                                string[] excludeSubDirectories = null,
                                string[] excludeFileExtensions = null,
                                int sysnchronizeDelayInMin = 60)
        {
            if (string.IsNullOrWhiteSpace(firstPath))
                throw new ArgumentException(nameof(firstPath));

            if (string.IsNullOrWhiteSpace(fileFilter))
                throw new ArgumentException(nameof(fileFilter));

            if (string.IsNullOrWhiteSpace(secondPath))
                throw new ArgumentException(nameof(secondPath));

            if (Directory.Exists(firstPath) == false)
            {
                Directory.CreateDirectory(firstPath);
            }
            if (Directory.Exists(secondPath) == false)
            {
                Directory.CreateDirectory(secondPath);
            }
            Data = new SynchronizerData(firstPath, fileFilter, secondPath, includeSubdirectories)
            IncludeSubdirectories = includeSubdirectories;
            _excludeSubDirectories.Add(StorageInfoFolder);
            _excludeSubDirectories.AddRange(excludeSubDirectories ?? new string[0]);
            _excludeFileExtensions.AddRange(excludeFileExtensions ?? new string[0]);

            SynchronizeDelay = sysnchronizeDelayInMin * 1000;
        }
        private List<StorageInfo> LoadCommonStorageInfos()
        {
            var result = new List<StorageInfo>();
            var firstStorageInfos = LoadData(FirstPath, StorageInfoFolder, StorageInfoFileName);
            var secondStorageInfos = LoadData(SecondPath, StorageInfoFolder, StorageInfoFileName);

            firstStorageInfos.ForEach(fiSi =>
            {
                var scSi = secondStorageInfos.SingleOrDefault(si => si.FileSubPath.Equals(fiSi.FileSubPath));

                if (scSi == null)
                {
                    result.Add(fiSi);
                }
                else
                {
                    if ((fiSi.LastWriteTime >= scSi.LastWriteTime))
                    {
                        result.Add(fiSi);
                    }
                    else
                    {
                        result.Add(scSi);
                    }
                }
            });
            return result;
        }
        private void SaveStorageFileInfos(List<StorageInfo> storageInfos)
        {
            SaveData(Path.Combine(FirstPath, StorageInfoFolder), StorageInfoFileName, storageInfos);
            SaveData(Path.Combine(SecondPath, StorageInfoFolder), StorageInfoFileName, storageInfos);
        }
        private List<StorageInfo> LoadData(string syncPath, string storageInfoFolder, string storageInfoFileName)
        {
            static string GetFileSubPath(string path, string filePath)
            {
                var fileSubPath = filePath.Replace(path, string.Empty);

                while (fileSubPath.StartsWith("\\"))
                {
                    fileSubPath = fileSubPath.Substring(1);
                }
                return fileSubPath;
            }
            static StorageInfo CreateStorageInfo(string path, FileInfo fileInfo)
            {
                return new StorageInfo
                {
                    SourcePath = path,
                    FileSubPath = GetFileSubPath(path, fileInfo.FullName),
                    LastWriteTime = fileInfo.LastWriteTime,
                };
            }

            var result = default(List<StorageInfo>);
            var storageInfoPath = Path.Combine(syncPath, storageInfoFolder);
            var storageInfoFilePath = Path.Combine(storageInfoPath, storageInfoFileName);

            if (Directory.Exists(storageInfoPath) == false)
            {
                Directory.CreateDirectory(storageInfoPath);
            }
            if (File.Exists(storageInfoFilePath))
            {
                var formatter = new BinaryFormatter();
                using var fs = new FileStream(storageInfoFilePath, FileMode.Open, FileAccess.Read);

                result = (List<StorageInfo>)formatter.Deserialize(fs);
                fs.Close();

                GetFileInfos(syncPath).ToList()
                                  .ForEach(fi =>
                {
                    var fileSubPath = GetFileSubPath(syncPath, fi.FullName);
                    var si = result.SingleOrDefault(i => i.FileSubPath.Equals(fileSubPath));

                    if (si == null)
                    {
                        result.Add(CreateStorageInfo(syncPath, fi));
                    }
                });
            }
            else
            {
                result = new List<StorageInfo>();

                GetFileInfos(syncPath).ToList()
                                  .ForEach(fi => result.Add(CreateStorageInfo(syncPath, fi)));
            }
            return result;
        }
        private void SaveData(string path, string fileName, List<StorageInfo> storageInfos)
        {
            var filePath = Path.Combine(path, fileName);

            if (Directory.Exists(path) == false)
            {
                Directory.CreateDirectory(path);
            }
            var formatter = new BinaryFormatter();
            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);

            formatter.Serialize(fs, storageInfos);
            fs.Close();
        }
        private IEnumerable<FileInfo> GetFileInfos(string path)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            FileInfo[] fis = directoryInfo.GetFiles(FileFilter, IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

            return fis.Where(fi => IsHandleFile(fi.FullName));
        }

        protected JsonSerializerOptions DeserializerOptions => new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        protected IEnumerable<T> LoadJsonData<T>(string dataFolder, string fileName)
        {
            var result = default(IEnumerable<T>);
            var path = Path.Combine(Directory.GetCurrentDirectory(), dataFolder);
            var filePath = Path.Combine(path, $"{fileName}.json");

            if (Directory.Exists(path) == false)
            {
                Directory.CreateDirectory(path);
            }
            else if (File.Exists(filePath))
            {
                result = JsonSerializer.Deserialize<IEnumerable<T>>(System.IO.File.ReadAllText(filePath), DeserializerOptions);
            }
            else
            {
                result = new List<T>();
            }
            return result;
        }
        protected void SaveJsonData<T>(IEnumerable<T> models, string dataFolder, string fileName)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), dataFolder);
            var filePath = Path.Combine(path, $"{fileName}.json");

            if (Directory.Exists(path) == false)
            {
                Directory.CreateDirectory(path);
            }
            File.WriteAllText(filePath, JsonSerializer.Serialize<IEnumerable<T>>(models));
        }

        public void Start()
        {
            if (synchronizeRunning == false)
            {
                var updateThread = new Thread(async () => await SynchronizePathsAsync());

                updateThread.IsBackground = true;
                updateThread.Start();
                synchronizeRunning = true;
            }
        }
        public void Stop()
        {
            synchronizeRunning = false;
        }

        private Task CreateFileAsync(string sourceFilePath, string targetFilePath)
        {
            return Task.Run(() =>
            {
                string targetDirectory = Path.GetDirectoryName(targetFilePath);

                if (Directory.Exists(targetDirectory) == false)
                {
                    Directory.CreateDirectory(targetDirectory);
                }
                if (Directory.Exists(targetDirectory) == true)
                {
                    CopyFileAsync(sourceFilePath, targetFilePath);
                }
            });
        }
        private Task CopyFileAsync(string sourceFilePath, string targetFilePath)
        {
            return Task.Run(() =>
            {
                bool tryCopy = true;

                while (tryCopy)
                {
                    var isLocked = IsFileLocked(sourceFilePath);

                    if (isLocked == null)
                    {
                        tryCopy = false;
                    }
                    else if (isLocked == false)
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
            });
        }
        private Task OverrideFileAsync(string sourceFilePath, string targetFilePath)
        {
            return Task.Run(() =>
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
        private Task DeleteFileItemAsync(string fileItem)
        {
            return Task.Run(() =>
            {
                if (File.Exists(fileItem) == true)
                {
                    bool tryDelete = true;

                    while (tryDelete)
                    {
                        var isLocked = IsFileLocked(fileItem);

                        if (isLocked == null)
                        {
                            tryDelete = false;
                        }
                        else if (isLocked == false)
                        {
                            ToLog($"{MethodBase.GetCurrentMethod().Name}: Delete {fileItem}");
                            File.Delete(fileItem);
                            tryDelete = false;
                        }
                        else
                        {
                            Thread.Sleep(FileOperationDelay);
                        }
                    }
                }
                else if (Directory.Exists(fileItem))
                {
                    ToLog($"{MethodBase.GetCurrentMethod().Name}: Delete {fileItem}");
                    Directory.Delete(fileItem, true);
                }
            });
        }

        private async Task SynchronizePathsAsync()
        {
            while (synchronizeRunning)
            {
                var storageInfos = LoadCommonStorageInfos();

                foreach (var storageInfo in storageInfos)
                {
                    await SynchronizeFileAsync(storageInfo);
                }
                SaveStorageFileInfos(storageInfos);
                Thread.Sleep(SynchronizeDelay);
            }
        }
        private async Task SynchronizeFileAsync(StorageInfo storageInfo)
        {
            var result = SynchronizeAction.None;
            var firstFilePath = Path.Combine(FirstPath, storageInfo.FileSubPath);
            var secondFilePath = Path.Combine(SecondPath, storageInfo.FileSubPath);

            if (storageInfo.LastSync == null)
            {
                if (storageInfo.SourcePath.Equals(FirstPath))
                {
                    await CopyFileAsync(firstFilePath, secondFilePath);

                }
                else
                {
                    await CopyFileAsync(secondFilePath, firstFilePath);
                }
            }
            else
            {
                if (storageInfo.SourcePath.Equals(FirstPath))
                {
                    if (File.Exists(firstFilePath))
                    {
                        await CopyFileAsync(firstFilePath, secondFilePath);
                    }
                    else
                    {
                        await DeleteFileItemAsync(secondFilePath);
                    }
                }
                else
                {
                    if (File.Exists(secondFilePath))
                    {
                        await CopyFileAsync(secondFilePath, firstFilePath);
                    }
                    else
                    {
                        await DeleteFileItemAsync(firstFilePath);
                    }
                }
            }
            storageInfo.LastSync = DateTime.Now;
        }

        private bool IsHandleFile(string filePath)
        {
            string ext = Path.GetExtension(filePath);

            return ExcludeSubDirectories.Any(e => filePath.ToLower().Contains($"\\{e.ToLower()}")) == false
                   && ExcludeFileExtensions.Any(i => i.Equals(ext, StringComparison.CurrentCultureIgnoreCase)) == false;
        }
        private bool IsHandleDirectory(string path)
        {
            return Directory.Exists(path)
                   && ExcludeSubDirectories.Any(e => path.ToLower().Contains($"\\{e.ToLower()}")) == false;
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
