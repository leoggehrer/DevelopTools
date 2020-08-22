using System;
using System.IO;
using FileService.Contracts;
using FileService.Logic;

namespace PathSynchronizer.ConApp
{
    class Program
    {
        static Program()
        {
            HomePath = (Environment.OSVersion.Platform == PlatformID.Unix ||
                        Environment.OSVersion.Platform == PlatformID.MacOSX)
                       ? Environment.GetEnvironmentVariable("HOME")
                       : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
            TargetRootPath = Path.Combine(HomePath, "Google Drive");
            SourceRootPath = Path.Combine(HomePath, "Documents");
        }
        private static string HomePath { get; }
        private static string TargetRootPath { get; set; }
        private static string SourceRootPath { get; set; }
        private static string[] ExcludeSubDirectories = new string[] { ".git", "node_modules", "bin", "obj" };

        private static int lastLogLen = 0;
        static void Main(string[] args)
        {
            //string QnSDevelopSourcePath = Path.Combine(SourceRootPath, "QnSDevelop");
            //string QnSDevelopTargetPath = Path.Combine(TargetRootPath, "QnSDevelop");
            string CSharpSourcePath = Path.Combine(SourceRootPath, "CSharp");
            string CSharpTargetPath = Path.Combine(SourceRootPath, "Schule", "CSharp");
            string AngularDemosSourcePath = Path.Combine(SourceRootPath, "AngularDemos");
            string AngularDemosTargetPath = Path.Combine(TargetRootPath, "Schule", "AngularDemos");
            string AngularExerciseSourcePath = Path.Combine(SourceRootPath, "AngularExercise");
            string AngularExerciseTargetPath = Path.Combine(TargetRootPath, "Schule", "AngularExercise");

            Console.Clear();
            Action<string> logger = t =>
            {
                Console.SetCursorPosition(0, 4);
                var saveColor = Console.ForegroundColor;

                Console.ForegroundColor = ConsoleColor.Yellow;
                for (int i = 0; i < lastLogLen; i++)
                {
                    Console.Write(" ");
                }
                lastLogLen = t.Length;
                Console.SetCursorPosition(0, 4);
                Console.Write(t);
                Console.ForegroundColor = saveColor;
            };
            PrintHeader();

            Factory.DefaultLogger = logger;
            Console.SetCursorPosition(0, 10);
            //Console.WriteLine($"{nameof(QnSDevelopSourcePath)}: {QnSDevelopSourcePath}");
            //Console.WriteLine($"{nameof(QnSDevelopTargetPath)}: {QnSDevelopTargetPath}");
            //IFileWatcher qnsDevelopSynchonizer = Factory.CreatePathSynchronizer(QnSDevelopSourcePath, "*.*", QnSDevelopTargetPath, true, ExcludeSubDirectories);
            //qnsDevelopSynchonizer.Start();

            Console.WriteLine($"{nameof(CSharpSourcePath)}: {CSharpSourcePath}");
            Console.WriteLine($"{nameof(CSharpTargetPath)}: {CSharpTargetPath}");
            IFileService csharpSynchonizer = Factory.CreatePathSynchronizer(CSharpSourcePath, "*.*", CSharpTargetPath, true, ExcludeSubDirectories);
            csharpSynchonizer.Start();

            //Console.WriteLine($"{nameof(AngularDemosSourcePath)}: {AngularDemosSourcePath}");
            //Console.WriteLine($"{nameof(AngularDemosTargetPath)}: {AngularDemosTargetPath}");
            //IFileService angularDemosSynchonizer = Factory.CreatePathSynchronizer(AngularDemosSourcePath, "*.*", AngularDemosTargetPath, true, ExcludeSubDirectories);
            //angularDemosSynchonizer.Start();

            //Console.WriteLine($"{nameof(AngularExerciseSourcePath)}: {AngularExerciseSourcePath}");
            //Console.WriteLine($"{nameof(AngularExerciseTargetPath)}: {AngularExerciseTargetPath}");
            //IFileService angularExerciseSynchonizer = Factory.CreatePathSynchronizer(AngularExerciseSourcePath, "*.*", AngularExerciseTargetPath, true, ExcludeSubDirectories);
            //angularExerciseSynchonizer.Start();

            PrintQuitMessage();
            Console.ReadKey();

            //qnsDevelopSynchonizer.Stop();
            csharpSynchonizer.Stop();
            //angularDemosSynchonizer.Stop();
            //angularExerciseSynchonizer.Stop();
        }

        static void PrintHeader()
        {
            Console.Clear();
            Console.SetCursorPosition(0, 0);
            Console.WriteLine($"{nameof(PathSynchronizer)}:");
            Console.WriteLine("==========================================");
            Console.WriteLine("LastAction:");
        }
        static void PrintQuitMessage()
        {
            Console.Write("Press any key to exit ...");
        }

    }
}
