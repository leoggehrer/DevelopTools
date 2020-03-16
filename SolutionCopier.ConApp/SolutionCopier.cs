using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace SolutionCopier.ConApp
{
    class SolutionCopier
    {
        public static Action<string> Logger { get; set; } = s => System.Diagnostics.Debug.WriteLine(s);

        private static char Separator { get; } = ';';
        private static string QnSBaseCodeLabel => "//@QnSBaseCode";
        private static string QnSCodeCopyLabel => "//@QnSCodeCopy";

        private static string[] IgnoreFolders = new string[]
        {
            "\\.vs"
            ,"\\.vs\\"
            ,"\\.git"
            ,"\\.git\\"
            ,"\\bin"
            ,"\\bin\\"
            ,"\\obj"
            ,"\\obj\\"
        };
        private static string[] ReplaceExtensions { get; } = new string[]
        {
            ".asax"
            ,".config"
            ,".cs"
            ,".cshtml"
            ,".csproj"
            ,".css"
            ,".html"
            ,".js"
            ,".json"
            ,".less"
            ,".sln"
            ,".tt"
            ,".txt"
            ,".xml"
            ,".razor"
            ,".md"
            ,".cd"
        };
        private List<string> Extensions { get; } = new List<string>();
        private List<string> ProjectGuids { get; } = new List<string>();
        public void Copy(string sourceDirectory, string targetDirectory)
        {
            if (string.IsNullOrWhiteSpace(sourceDirectory) == true)
                throw new ArgumentException(nameof(sourceDirectory));

            if (string.IsNullOrWhiteSpace(targetDirectory) == true)
                throw new ArgumentException(nameof(targetDirectory));

            Logger($"Source-Project: {sourceDirectory}");
            Logger($"Target-Directory: {targetDirectory}");

            if (sourceDirectory.Equals(targetDirectory) == false)
            {
                Logger("Running");
                var result = CreateTemplate(sourceDirectory, targetDirectory);

                foreach (var ext in Extensions.OrderBy(i => i))
                {
                    System.Diagnostics.Debug.WriteLine($",\"{ext}\"");
                }

                if (result)
                {
                    Logger("Finished!");
                }
                else
                {
                    Logger("Not finished! There are some errors!");
                }
            }
        }

        private bool CreateTemplate(string sourceDirectory, string targetDirectory)
        {
            if (Directory.Exists(targetDirectory) == false)
            {
                Directory.CreateDirectory(targetDirectory);
            }

            string sourceFolderName = new DirectoryInfo(sourceDirectory).Name;
            string targetFolderName = new DirectoryInfo(targetDirectory).Name;

            CopySolutionStructure(sourceDirectory, targetDirectory);

            foreach (var directory in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                string subFolder = directory.Replace(sourceDirectory, string.Empty);

                subFolder = subFolder.Replace(sourceFolderName, targetFolderName);

                if (IgnoreFolders.Any(i => subFolder.EndsWith(i) || subFolder.Contains(i)) == false)
                {
                    CopyDirectoryWorkFiles(directory, targetDirectory, sourceFolderName, targetFolderName);
                }
            }
            return true;
        }
        private void CopySolutionFile(string sourceSolutionFilePath, string targetSolutionFilePath)
        {
            string sourceSolutionFileName = Path.GetFileNameWithoutExtension(sourceSolutionFilePath);
            string targetSolutionFileName = Path.GetFileNameWithoutExtension(targetSolutionFilePath);

            CopyFile(sourceSolutionFilePath, targetSolutionFilePath, sourceSolutionFileName, targetSolutionFileName);
        }
        private void CopyProjectFiles(string sourceDirectory, string targetDirectory, string sourceFolderName, string targetFolderName)
        {
            string projectFilePath = string.Empty;

            foreach (var sourceFile in new DirectoryInfo(sourceDirectory).GetFiles("*.csproj", SearchOption.AllDirectories))
            {
                string targetFilePath = CreateTargetFilePath(sourceFile.FullName, sourceDirectory, targetDirectory, sourceFolderName, targetFolderName);

                CopyFile(sourceFile.FullName, targetFilePath, sourceFolderName, targetFolderName);
            }
            if (string.IsNullOrEmpty(projectFilePath) == false)
            {
                ReplaceProjectGuids(projectFilePath);
            }
        }
        private void CopyDirectoryWorkFiles(string sourceDirectory, string targetDirectory, string sourceFolderName, string targetFolderName)
        {
            string projectFilePath = string.Empty;

            foreach (var sourceFile in new DirectoryInfo(sourceDirectory).GetFiles("*")
                                                                         .Where(f => ReplaceExtensions.Any(i => i.Equals(Path.GetExtension(f.Name)))))
            {
                string targetFilePath = CreateTargetFilePath(sourceFile.FullName, sourceDirectory, targetDirectory, sourceFolderName, targetFolderName);

                CopyFile(sourceFile.FullName, targetFilePath, sourceFolderName, targetFolderName);
            }
        }
        private void CopySolutionStructure(string sourceDirectory, string targetDirectory)
        {
            string sourceFolderName = new DirectoryInfo(sourceDirectory).Name;
            string targetFolderName = new DirectoryInfo(targetDirectory).Name;
            string sourceSolutionFilePath = Directory.GetFiles(sourceDirectory, "*.sln", SearchOption.AllDirectories)
                                                     .FirstOrDefault(f => f.EndsWith($"{sourceFolderName}.sln", StringComparison.CurrentCultureIgnoreCase));
            string sourceSolutionPath = Path.GetDirectoryName(sourceSolutionFilePath);
            string targetSolutionPath = sourceSolutionPath.Replace(sourceDirectory, targetDirectory);
            string targetSolutionFilePath = CreateTargetFilePath(sourceSolutionFilePath, sourceDirectory, targetDirectory, sourceFolderName, targetFolderName);

            CopySolutionFile(sourceSolutionFilePath, targetSolutionFilePath);
            CopyProjectFiles(sourceDirectory, targetDirectory, sourceFolderName, targetFolderName);
        }
        private void CopyFile(string sourceFilePath, string targetFilePath, string sourceSolutionName, string targetSolutionName)
        {
            string extension = Path.GetExtension(sourceFilePath);
            string targetDirectory = Path.GetDirectoryName(targetFilePath);

            if (Extensions.SingleOrDefault(i => i.Equals(extension, StringComparison.CurrentCultureIgnoreCase)) == null)
            {
                Extensions.Add(extension);
            }

            if (targetDirectory != null
                && Directory.Exists(targetDirectory) == false)
            {
                Directory.CreateDirectory(targetDirectory);
            }

            if (ReplaceExtensions.SingleOrDefault(i => i.Equals(extension, StringComparison.CurrentCultureIgnoreCase)) != null)
            {
                string[] sourceLines = File.ReadAllLines(sourceFilePath, Encoding.Default);
                List<string> targetLines = new List<string>();
                Regex regex = new Regex(sourceSolutionName, RegexOptions.IgnoreCase);

                if (sourceFilePath.EndsWith("BlazorApp.csproj"))
                {
                    for (int i = 0; i < sourceLines.Length; i++)
                    {
                        var sourceLine = sourceLines[i];

                        if (sourceLine.TrimStart().StartsWith("<UserSecretsId>"))
                        {
                            sourceLine = $"    <UserSecretsId>{Guid.NewGuid()}</UserSecretsId>";
                            sourceLines[i] = sourceLine;
                        }
                    }
                }

                foreach (var sourceLine in sourceLines)
                {
                    var targetLine = regex.Replace(sourceLine, targetSolutionName);

                    targetLine = targetLine.Replace(QnSBaseCodeLabel, QnSCodeCopyLabel);
                    targetLines.Add(targetLine);
                }
                File.WriteAllLines(targetFilePath, targetLines.ToArray(), Encoding.Default);
            }
            else if (File.Exists(targetFilePath) == false)
            {
                File.Copy(sourceFilePath, targetFilePath);
            }
        }

        private string CreateTargetFilePath(string sourceFileName, string sourceDirectory, string targetDirectory, string sourceFolderName, string targetFolderName)
        {
            string targetFileName = sourceFileName.Replace(sourceFolderName, targetFolderName);
            string targetFilePath = targetDirectory;
            string subPath = Path.GetDirectoryName(sourceFileName).Replace(sourceDirectory, string.Empty);

            foreach (var item in subPath.Split('\\'))
            {
                targetFilePath = Path.Combine(targetFilePath, item.Replace(sourceFolderName, targetFolderName));
            }
            return Path.Combine(targetFilePath, targetFileName);
        }
        private void ReplaceProjectGuids(string filePath)
        {
            XmlDocument xml = new XmlDocument();

            xml.Load(filePath);

            if (xml.DocumentElement != null)
            {
                foreach (XmlNode node in xml.DocumentElement.ChildNodes)
                {
                    // first node is the url ... have to go to nexted loc node
                    foreach (XmlNode item in node)
                    {
                        if (item.Name.Equals("ProjectGuid") == true)
                        {
                            string newGuid = Guid.NewGuid().ToString().ToUpper();

                            ProjectGuids.Add($"{item.InnerText}{Separator}{newGuid}");

                            item.InnerText = "{" + newGuid + "}";
                        }
                    }
                }
            }
            xml.Save(filePath);
        }
    }
}
