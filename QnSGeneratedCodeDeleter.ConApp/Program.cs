using System;
using System.IO;
using DevelopCommon;

namespace QnSGeneratedCodeDeleter.ConApp
{
    internal class Program
    {
        private static string[] Paths => new string[] {
            @"C:\Develop\QnSDevelopForBusiness\QuickNSmart\source\QuickNSmart"
        };
        private static string[] SearchPatterns => StaticLiterals.SearchPattern.Split('|');

        private static void Main(string[] args)
        {
            string input;
            string queryMsg = "Delete generated code files [y|n]?: ";

            PrintHeader();
            Console.Write(queryMsg);
            input = Console.ReadLine();
            while (input.Equals("y", StringComparison.CurrentCultureIgnoreCase))
            {
                Console.Clear();
                PrintHeader();

                foreach (var path in Paths)
                {
                    // Delete all QnSGeneratedCode files
                    foreach (var searchPattern in SearchPatterns)
                    {
                        var sourceFiles = Solution.GetSourceCodeFiles(path, searchPattern, new string[] { StaticLiterals.QnSGeneratedCodeLabel });

                        foreach (var sourceFile in sourceFiles)
                        {
                            Console.Write(".");
                            System.Threading.Thread.Sleep(100);
                            File.Delete(sourceFile);
                        }
                    }
                }
                PrintHeader();
                Console.Write(queryMsg);
                input = Console.ReadLine();
            }
        }

        private static void PrintHeader()
        {
            Console.Clear();
            Console.SetCursorPosition(0, 0);
            Console.WriteLine($"{nameof(QnSGeneratedCodeDeleter)}:");
            Console.WriteLine("==================================");
            Console.WriteLine();
            foreach (var path in Paths)
            {
                Console.WriteLine($"Directory: {path}");
            }
            Console.WriteLine();
        }
    }
}
