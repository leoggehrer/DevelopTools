﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BaseCodeCopier.ConApp
{
	class Program
	{
		static Program()
		{
			HomePath = (Environment.OSVersion.Platform == PlatformID.Unix ||
						Environment.OSVersion.Platform == PlatformID.MacOSX)
					   ? Environment.GetEnvironmentVariable("HOME")
					   : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");

			Paths = new Dictionary<string, string[]>();
			SourceLabels = new Dictionary<string, string[]>();
			// Project: MusicStore
			var sourcePath = Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "MusicStorePartA", "Solution", "MusicStore");
			var targetPaths = new string[]
			{
				Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "MusicStorePartB", "Solution", "MusicStore"),
				Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "MusicStorePartC", "Solution", "MusicStore"),
				Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "MusicStorePartD", "Solution", "MusicStore"),
			};
			Paths.Add(sourcePath, targetPaths);
			SourceLabels.Add(sourcePath, new string[] { BaseCodeLabel, DomainCodeLabel });

			sourcePath = Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "MusicStorePartB", "Solution", "MusicStore");
			targetPaths = new string[]
			{
				Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "MusicStorePartC", "Solution", "MusicStore"),
				Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "MusicStorePartD", "Solution", "MusicStore"),
			};
			Paths.Add(sourcePath, targetPaths);
			SourceLabels.Add(sourcePath, new string[] { BaseCodeLabel, DomainCodeLabel });

			sourcePath = Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "MusicStorePartC", "Solution", "MusicStore");
			targetPaths = new string[]
			{
				Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "MusicStorePartD", "Solution", "MusicStore"),
			};
			Paths.Add(sourcePath, targetPaths);
			SourceLabels.Add(sourcePath, new string[] { BaseCodeLabel, DomainCodeLabel });
			// End: MusicStore

			// Project: HolidayCount

			// End: HolidayCount
		}
		private static string HomePath { get; }
		private static Dictionary<string, string[]> Paths { get; set; }
		private static Dictionary<string, string[]> SourceLabels { get; set; }
		private static string SearchPattern => "*.cs";
		private static string BaseCodeLabel => "//@BaseCode";
		private static string DomainCodeLabel => "//@DomainCode";
		private static string CodeCopyLabel => "//@CodeCopy";
		private static string[] Projects { get; } = new string[] { "CommonBase" };
		private static string[] ProjectExtensions { get; } = new string[] { ".Contracts", ".Logic", ".Transfer", ".WebApi", ".Adapters" };
		static void Main(string[] args)
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
					var sourceLabels = SourceLabels[path.Key];
					var sourceCodeFiles = GetSourceCodeFiles(path.Key, SearchPattern, sourceLabels);

					foreach (var targetPath in path.Value)
					{
						foreach (var sourceCodeFile in sourceCodeFiles)
						{
							CopySourceCodeFile(path.Key, sourceCodeFile, targetPath);
						}
					}
				}
				Console.Write(queryMsg);
				input = Console.ReadLine();
			}
		}
		private static string GetSolutionName(string path)
		{
			var result = string.Empty;
			var data = path.Split("\\");

			if (data.Any())
			{
				result = data.Last();
			}
			return result;
		}
		private static string GetProjectName(string filePath, string solutionName)
		{
			var result = string.Empty;
			var data = filePath.Split("\\");

			for (int i = 0; i < data.Length && result == string.Empty; i++)
			{
				for (int j = 0; j < Projects.Length; j++)
				{
					if (data[i].Equals(Projects[j]))
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
		private static bool CopySourceCodeFile(string sourcePath, string baseCodeFilePath, string targetPath)
		{
			bool result = false;
			bool canCopy = true;
			string sourceSolutionName = GetSolutionName(sourcePath);
			string targetSolutionName = GetSolutionName(targetPath);
			string sourceProjectName = GetProjectName(baseCodeFilePath, sourceSolutionName);
			string targetProjectName = sourceProjectName.Replace(sourceSolutionName, targetSolutionName);
			string targetFilePath = baseCodeFilePath.Replace(sourcePath, targetPath).Replace(sourceProjectName, targetProjectName);
			string targetFileFolder = Path.GetDirectoryName(targetFilePath);

			if (Directory.Exists(targetFileFolder) == false)
			{
				Directory.CreateDirectory(targetFileFolder);
			}
			if (File.Exists(targetFilePath))
			{
				var lines = File.ReadAllLines(targetFilePath, Encoding.Default);

				if (lines.Any() && lines.First().StartsWith(CodeCopyLabel) == false)
				{
					canCopy = false;
				}
			}
			if (canCopy)
			{
				var lines = new List<string>();

				lines.Add(CodeCopyLabel);
				lines.AddRange(File.ReadAllLines(baseCodeFilePath, Encoding.Default)
								   .Skip(1)
								   .Select(i => i.Replace(sourceSolutionName, targetSolutionName)));
				File.WriteAllLines(targetFilePath, lines.ToArray(), Encoding.Default);
			}
			return result;
		}
		private static IEnumerable<string> GetSourceCodeFiles(string path, string searchPattern, string[] labels)
		{
			List<string> result = new List<string>();

			foreach (var file in Directory.GetFiles(path, searchPattern, SearchOption.AllDirectories))
			{
				var lines = File.ReadAllLines(file, Encoding.Default);

				if (lines.Any() && labels.Any(l => lines.First().StartsWith(l)))
				{
					result.Add(file);
				}
			}
			return result;
		}
		static void PrintHeader()
		{
			Console.Clear();
			Console.SetCursorPosition(0, 0);
			Console.WriteLine($"{nameof(BaseCodeCopier)}:");
			Console.WriteLine("==========================================");
			Console.WriteLine();
			foreach (var path in Paths)
			{
				Console.WriteLine($"Source: {path.Key}");
				foreach (var target in path.Value)
				{
					Console.WriteLine($"\t{path.Key} -> {target}");
				}
			}
			Console.WriteLine();
		}
	}
}