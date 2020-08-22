using DevelopCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace BuildDockerfile.ConApp
{
    class Program
    {
        static Program()
        {
            HomePath = (Environment.OSVersion.Platform == PlatformID.Unix ||
                        Environment.OSVersion.Platform == PlatformID.MacOSX)
                       ? Environment.GetEnvironmentVariable("HOME")
                       : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");

            Paths.Add(Path.Combine(HomePath, "Google Drive", "Schule", "CSharp"));
        }

        private static string HomePath { get; }
        private static List<string> Paths { get; } = new List<string>();
        private static List<string> Dockerfiles { get; } = new List<string>();
        static void Main(string[] args)
        {
            bool running = false;

            do
            {
                PrintHeader();
                Console.Write($"Build [1..{Dockerfiles.Count + 1}|X]?: ");
                var input = Console.ReadLine();

                running = input.Equals("x", StringComparison.CurrentCultureIgnoreCase) == false;
                if (running)
                {
                    var numbers = input.Split(',').Where(s => Int32.TryParse(s, out int n))
                                       .Select(s => Int32.Parse(s))
                                       .ToArray();

                    foreach (var number in numbers)
                    {
                        if (number == Dockerfiles.Count + 1)
                        {
                            BuildDockerfiles(Paths);
                        }
                        else if (number > 0 && number <= Dockerfiles.Count)
                        {
                            BuildDockerfile(Dockerfiles[number - 1]);
                        }
                    }
                }
            } while (running);
        }
        static void PrintHeader()
        {
            int index = 0;

            Console.Clear();
            Console.SetCursorPosition(0, 0);
            Console.WriteLine($"{nameof(BuildDockerfile)}:");
            Console.WriteLine("==========================================");
            Console.WriteLine();

            Dockerfiles.Clear();
            foreach (var path in Paths)
            {
                foreach (var dockerfile in Solution.GetDockerfiles(path))
                {
                    FileInfo dockerfileInfo = new FileInfo(dockerfile);
                    string directoryName = dockerfileInfo.Directory.Name;

                    Dockerfiles.Add(dockerfileInfo.FullName);
                    Console.WriteLine($"Build docker image for: [{++index,2}] {directoryName}");
                }
            }
            Console.WriteLine($"Build docker image for: [{++index,2}] ALL");
            Console.WriteLine();
        }
        static void BuildDockerfile(string dockerfile)
        {
            var maxWaiting = 10 * 60 * 1000;    // 10 minutes
            var slnPath = Directory.GetParent(Path.GetDirectoryName(dockerfile)).FullName;
            var csprojFile = Solution.GetContractProjectFileFromDockerfile(dockerfile);
            var csprojLines = default(string[]);

            if (string.IsNullOrEmpty(csprojFile) == false)
            {
                csprojLines = File.ReadAllLines(csprojFile, Encoding.Default);
                try
                {
                    //RUN dotnet build "QnSIdentityServer.WebApi.csproj" - c Release - o / app / build
                    ProcessStartInfo csprojStartInfo = new ProcessStartInfo("dotnet.exe")
                    {
                        Arguments = $"build \"{csprojFile}\" -c Release",
                        //WorkingDirectory = projectPath,
                        UseShellExecute = false
                    };
                    Process.Start(csprojStartInfo).WaitForExit(maxWaiting);
                    File.WriteAllLines(csprojFile, csprojLines.Select(l => l.Replace("Condition=\"True\"", "Condition=\"False\"")), Encoding.Default);
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Error: {e.Message}");
                }
            }
            FileInfo dockerfileInfo = new FileInfo(dockerfile);
            //docker build -f "c:\users\g.gehrer\google drive\schule\csharp\qnsidentityserver\solution\qnsidentityserver\qnsidentityserver.webapi\dockerfile" --force-rm -t qnsidentityserverwebapi  --label "com.microsoft.created-by=visual-studio" --label "com.microsoft.visual-studio.project-name=QnSIdentityServer.WebApi" "c:\users\g.gehrer\google drive\schule\csharp\qnsidentityserver\solution\qnsidentityserver"
            var directoryName = dockerfileInfo.Directory.Name;
            var directoryFullName = dockerfileInfo.Directory.FullName;
            var arguments = $"build -f \"{dockerfile}\" --force-rm -t {directoryName.Replace(".", string.Empty).ToLower()}  --label \"com.microsoft.created-by=visual-studio\" --label \"com.microsoft.visual-studio.project-name={directoryName}\" \"{slnPath}\"";
            Console.WriteLine(arguments);
            ProcessStartInfo buildStartInfo = new ProcessStartInfo("docker")
            {
                Arguments = arguments,
                WorkingDirectory = directoryFullName,
                UseShellExecute = false
            };
            try
            {
                Process.Start(buildStartInfo).WaitForExit(maxWaiting);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error: {e.Message}");
            }
            if (csprojLines != null)
            {
                File.WriteAllLines(csprojFile, csprojLines, Encoding.Default);
            }
        }
        static void BuildDockerfiles(IEnumerable<string> paths)
        {
            int maxWaiting = 10 * 60 * 1000;    // 10 minutes
            foreach (var path in paths)
            {
                var slnPaths = Solution.GetSolutionPaths(path);

                foreach (var slnPath in slnPaths)
                {
                    var csprojFile = default(string);
                    var csprojLines = default(string[]);
                    var dockerfiles = Solution.GetDockerfiles(slnPath);

                    if (dockerfiles.Any())
                    {
                        csprojFile = Solution.GetContractProjectFileFromSolutionPath(slnPath);

                        if (string.IsNullOrEmpty(csprojFile) == false)
                        {
                            csprojLines = File.ReadAllLines(csprojFile, Encoding.Default);
                            try
                            {
                                //RUN dotnet build "QnSIdentityServer.WebApi.csproj" - c Release - o / app / build
                                ProcessStartInfo startInfo = new ProcessStartInfo("dotnet.exe")
                                {
                                    Arguments = $"build \"{csprojFile}\" -c Release",
                                    //WorkingDirectory = projectPath,
                                    UseShellExecute = false
                                };
                                Process.Start(startInfo).WaitForExit(maxWaiting);
                                File.WriteAllLines(csprojFile, csprojLines.Select(l => l.Replace("Condition=\"True\"", "Condition=\"False\"")), Encoding.Default);
                            }
                            catch (Exception e)
                            {
                                Debug.WriteLine($"Error: {e.Message}");
                            }
                        }
                    }
                    foreach (var dockerfile in dockerfiles)
                    {
                        FileInfo dockerfileInfo = new FileInfo(dockerfile);
                        //docker build -f "c:\users\g.gehrer\google drive\schule\csharp\qnsidentityserver\solution\qnsidentityserver\qnsidentityserver.webapi\dockerfile" --force-rm -t qnsidentityserverwebapi  --label "com.microsoft.created-by=visual-studio" --label "com.microsoft.visual-studio.project-name=QnSIdentityServer.WebApi" "c:\users\g.gehrer\google drive\schule\csharp\qnsidentityserver\solution\qnsidentityserver"
                        string directoryName = dockerfileInfo.Directory.Name;
                        string directoryFullName = dockerfileInfo.Directory.FullName;
                        string arguments = $"build -f \"{dockerfile}\" --force-rm -t {directoryName.Replace(".", string.Empty).ToLower()}  --label \"com.microsoft.created-by=visual-studio\" --label \"com.microsoft.visual-studio.project-name={directoryName}\" \"{slnPath}\"";
                        ProcessStartInfo startInfo = new ProcessStartInfo("docker")
                        {
                            Arguments = arguments,
                            WorkingDirectory = directoryFullName,
                            UseShellExecute = false
                        };
                        try
                        {
                            Process.Start(startInfo).WaitForExit(maxWaiting);
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine($"Error: {e.Message}");
                        }
                    }
                    if (csprojLines != null)
                    {
                        File.WriteAllLines(csprojFile, csprojLines, Encoding.Default);
                    }
                }
            }
        }
    }
}
