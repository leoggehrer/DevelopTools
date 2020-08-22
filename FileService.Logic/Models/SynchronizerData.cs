using System;
using System.Collections.Generic;

namespace FileService.Logic.Models
{
    [Serializable]
    public class SynchronizerData : ICloneable
    {
        public DateTime? LastSync { get; set; }
        public string FirstPath { get; set; }
        public string SecondPath { get; set;  }

        public string FileFilter { get; set; }
        public bool IncludeSubdirectories { get; set; }
        public List<string> ExcludeSubDirectories { get; set; } = new List<string>();
        public List<string> ExcludeFileExtensions { get; set; } = new List<string>();

        public int SynchronizeDelay { get; }

        public SynchronizerData(string firstPath,
                                string fileFilter,
                                string secondPath,
                                bool includeSubdirectories = true,
                                IEnumerable<string> excludeSubDirectories = null,
                                IEnumerable<string> excludeFileExtensions = null,
                                int sysnchronizeDelayInMin = 60)
        {
            if (string.IsNullOrWhiteSpace(firstPath))
                throw new ArgumentException(nameof(firstPath));

            if (string.IsNullOrWhiteSpace(fileFilter))
                throw new ArgumentException(nameof(fileFilter));

            if (string.IsNullOrWhiteSpace(secondPath))
                throw new ArgumentException(nameof(secondPath));

            FirstPath = firstPath;
            FileFilter = fileFilter;
            SecondPath = secondPath;

            IncludeSubdirectories = includeSubdirectories;
            ExcludeSubDirectories.AddRange(excludeSubDirectories ?? new string[0]);
            ExcludeFileExtensions.AddRange(excludeFileExtensions ?? new string[0]);

            SynchronizeDelay = sysnchronizeDelayInMin * 1000;
        }

        public override bool Equals(object obj)
        {
            var result = false;

            if (obj is SynchronizerData other)
            {
                if ((FirstPath.Equals(other.FirstPath, StringComparison.CurrentCultureIgnoreCase) 
                     && SecondPath.Equals(other.SecondPath, StringComparison.CurrentCultureIgnoreCase))
                     ||
                    (FirstPath.Equals(other.FirstPath, StringComparison.CurrentCultureIgnoreCase)
                     && SecondPath.Equals(other.SecondPath, StringComparison.CurrentCultureIgnoreCase))
                    )
                {
                    result = true;
                }
            }
            return result;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(FirstPath, SecondPath);
        }

        public object Clone()
        {
            return new SynchronizerData(FirstPath,
                                        FileFilter,
                                        SecondPath,
                                        IncludeSubdirectories,
                                        ExcludeSubDirectories,
                                        ExcludeFileExtensions,
                                        SynchronizeDelay / 1000)
            {
                LastSync = LastSync,
            };
        }
    }
}
