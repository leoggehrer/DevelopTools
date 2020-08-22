using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DevelopCommon
{
    public static partial class Solution
    {
        public static string[] CommonProjects { get; } = new string[] { "CommonBase", "CSharpCodeGenerator.ConApp" };
        public static string[] ProjectExtensions { get; } = new string[] { ".Contracts", ".Logic", ".Transfer", ".WebApi", ".Adapters", ".AspMvc", ".BlazorApp", ".ConApp" };

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
        public static string GetSolutionNameFromPath(string path)
        {
            var result = string.Empty;
            var data = path.Split("\\");

            if (data.Any())
            {
                result = data.Last();
            }
            return result;
        }
        public static string GetProjectNameFromFilePath(string filePath, string solutionName)
        {
            var result = string.Empty;
            var data = filePath.Split("\\");

            for (int i = 0; i < data.Length && result == string.Empty; i++)
            {
                for (int j = 0; j < CommonProjects.Length; j++)
                {
                    if (data[i].Equals(CommonProjects[j]))
                    {
                        result = data[i];
                    }
                }
                if (string.IsNullOrEmpty(result))
                {
                    for (int j = 0; j < ProjectExtensions.Length; j++)
                    {
                        if (data[i].Equals($"{solutionName}{ProjectExtensions[j]}"))
                        {
                            result = data[i];
                        }
                    }
                }
            }
            return result;
        }

        public static IEnumerable<string> GetSourceCodeFiles(string path, string searchPattern, string[] labels)
        {
            List<string> result = new List<string>();

            foreach (var file in Directory.GetFiles(path, searchPattern, SearchOption.AllDirectories))
            {
                var lines = File.ReadAllLines(file, Encoding.Default);

                if (lines.Any() && labels.Any(l => lines.First().Contains(l)))
                {
                    result.Add(file);
                }
                System.Diagnostics.Debug.WriteLine($"{file}");
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
