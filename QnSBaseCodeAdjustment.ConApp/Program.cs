using DevelopCommon;
using System;
using System.Collections.Generic;
using System.IO;

namespace QnSBaseCodeAdjustment.ConApp
{
    internal class Program
    {
        static Program()
        {
            HomePath = (Environment.OSVersion.Platform == PlatformID.Unix ||
                        Environment.OSVersion.Platform == PlatformID.MacOSX)
                       ? Environment.GetEnvironmentVariable("HOME")
                       : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");

            Paths = new Dictionary<string, string[]>();
            SourceLabels = new Dictionary<string, string[]>();

            // Project: QuickNSmart-Projects
            var basePath = @"C:\Develop\QnSDevelopForBusiness";
            var qnsSourcePath = @"QuickNSmart\source\QuickNSmart";
            var qnsHungryPath = @"QnSHungryLama\source\QnSHungryLama";
            var sourcePath = Path.Combine(basePath, qnsSourcePath);
            var targetPaths = new string[]
            {
                Path.Combine(basePath, qnsHungryPath),
            };
            Paths.Add(sourcePath, targetPaths);
            SourceLabels.Add(sourcePath, new string[] { StaticLiterals.QnSBaseCodeLabel });
            // End: QuickNSmart-Projects
        }
        private static string HomePath { get; }
        private static Dictionary<string, string[]> Paths { get; set; }
        private static Dictionary<string, string[]> SourceLabels { get; set; }
        private static string[] SearchPatterns => StaticLiterals.SearchPattern.Split('|');
        private static string DomainCodeLabel => "@DomainCode";
        private static string[] TargetLabels = new string[] { StaticLiterals.QnSCodeCopyLabel };

        private static void Main(string[] args)
        {
            string input;
            string queryMsg = "Copy [y|n]?: ";

            PrintHeader();
            Console.Write(queryMsg);
            input = Console.ReadLine();
            while (input.Equals("y", StringComparison.CurrentCultureIgnoreCase))
            {
                Console.Clear();
                PrintHeader();

                foreach (var path in Paths)
                {
                    // Delete all QnSCopyCode files
                    foreach (var targetPath in path.Value)
                    {
                        foreach (var searchPattern in SearchPatterns)
                        {
                            var targetCodeFiles = Solution.GetSourceCodeFiles(targetPath, searchPattern, TargetLabels);
                            foreach (var targetCodeFile in targetCodeFiles)
                            {
                                File.Delete(targetCodeFile);
                            }
                        }
                    }

                    // Copy all QnSBasCode files
                    foreach (var searchPattern in SearchPatterns)
                    {
                        var sourceLabels = SourceLabels[path.Key];
                        var sourceCodeFiles = Solution.GetSourceCodeFiles(path.Key, searchPattern, sourceLabels);

                        foreach (var targetPath in path.Value)
                        {
                            foreach (var sourceCodeFile in sourceCodeFiles)
                            {
                                Copier.SynchronizeSourceCodeFile(path.Key, sourceCodeFile, targetPath, sourceLabels, TargetLabels);
                                //Copier.CopySourceCodeFile(path.Key, sourceCodeFile, targetPath, sourceLabels, TargetLabels);
                            }
                        }
                    }
                }
                Console.Write(queryMsg);
                input = Console.ReadLine();
            }
        }

        private static void PrintHeader()
        {
            Console.Clear();
            Console.SetCursorPosition(0, 0);
            Console.WriteLine($"{nameof(QnSBaseCodeAdjustment)}:");
            Console.WriteLine("==========================================");
            Console.WriteLine();
            foreach (var path in Paths)
            {
                Console.WriteLine($"Source: {path.Key}");
                foreach (var target in path.Value)
                {
                    Console.WriteLine($"\t -> {target}");
                }
            }
            Console.WriteLine();
        }
    }
}
