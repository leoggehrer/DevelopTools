using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DevelopCommon
{
    public static partial class Solution
    {
        public static IEnumerable<string> GetAppSettings(string path)
        {
            List<string> result = new List<string>();

            foreach (var item in Directory.GetFiles(path, "appsettings.json", SearchOption.AllDirectories))
            {
                result.Add(item);
            }
            return result;
        }
        public static IEnumerable<string> GetDockerfiles(string path)
        {
            List<string> result = new List<string>();

            foreach (var item in Directory.GetFiles(path, "Dockerfile", SearchOption.AllDirectories))
            {
                result.Add(item);
            }
            return result;
        }

        public static IEnumerable<string> GetSolutionPaths(string path)
        {
            List<string> result = new List<string>();

            foreach (var item in Directory.GetFiles(path, "*.sln", SearchOption.AllDirectories))
            {
                result.Add(Path.GetDirectoryName(item));
            }
            return result;
        }
        public static string GetContractProjectFileFromSolutionPath(string path)
        {
            return Directory.GetFiles(path, "*.Contracts.*proj", SearchOption.AllDirectories).FirstOrDefault();
        }
        public static string GetContractProjectFileFromDockerfile(string dockerfile)
        {
            var path = Path.GetDirectoryName(dockerfile);
            var dirInfo = Directory.GetParent(path);
            var fileInfo = dirInfo.GetFiles("*.Contracts.*proj", SearchOption.AllDirectories).FirstOrDefault();

            return fileInfo?.FullName;
        }
    }
}
