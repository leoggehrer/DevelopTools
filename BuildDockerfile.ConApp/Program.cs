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

			Paths = new List<string>();
			Paths.Add(Path.Combine(HomePath, "Google Drive", "Schule", "CSharp"));
		}

		private static string HomePath { get; }
		private static List<string> Paths { get; set; }
		static void Main(string[] args)
        {
			bool running = false;
			string input;
			string queryMsg = "Copy [y|n]?: ";

			do
			{
				PrintHeader();
				Console.Write(queryMsg);
				input = Console.ReadLine();
				running = input.Equals("y", StringComparison.CurrentCultureIgnoreCase);
				if (running)
				{
					BuildDockerfiles(Paths);
				}
			} while (running);
		}
		static void PrintHeader()
		{
			Console.Clear();
			Console.SetCursorPosition(0, 0);
			Console.WriteLine($"{nameof(BuildDockerfile)}:");
			Console.WriteLine("==========================================");
			Console.WriteLine();
			foreach (var path in Paths)
			{
				var dockerFiles = GetDockerfiles(path);

				foreach (var dockerFile in dockerFiles)
				{
					FileInfo dockerfileInfo = new FileInfo(dockerFile);
					string directoryName = dockerfileInfo.Directory.Name;

					Console.WriteLine($"Build docker image for: {directoryName}");
				}
			}
			Console.WriteLine();
		}
		static void BuildDockerfiles(IEnumerable<string> paths)
		{
			int maxWaiting = 10 * 60 * 1000;	// 10 minutes
			foreach (var path in paths)
			{
				var slnPaths = GetSolutionPaths(path);

				foreach (var slnPath in slnPaths)
				{
					var csprojFile = default(string);
					var csprojLines = default(string[]);
					var dockerfiles = GetDockerfiles(slnPath);

					if (dockerfiles.Any())
					{
						csprojFile = GetContractProjectFile(slnPath);

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
		static IEnumerable<string> GetSolutionPaths(string path)
		{
			List<string> result = new List<string>();

			foreach (var item in Directory.GetFiles(path, "*.sln", SearchOption.AllDirectories))
			{
				result.Add(Path.GetDirectoryName(item));
			}
			return result;
		}
		static IEnumerable<string> GetDockerfiles(string path)
		{
			List<string> result = new List<string>();

			foreach (var item in Directory.GetFiles(path, "Dockerfile", SearchOption.AllDirectories))
			{
				result.Add(item);
			}
			return result;
		}
		static string GetContractProjectFile(string path)
		{
			return Directory.GetFiles(path, "*.Contracts.*proj", SearchOption.AllDirectories).FirstOrDefault();
		}
	}
}
