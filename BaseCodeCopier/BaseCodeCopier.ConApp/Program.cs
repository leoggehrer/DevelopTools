using System;
using System.Collections.Generic;
using System.IO;
using DevelopCommon;

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
			//// Project: MusicStore
			//var sourcePath = Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "MusicStorePartA", "Solution", "MusicStore");
			//var targetPaths = new string[]
			//{
			//	Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "MusicStorePartB", "Solution", "MusicStore"),
			//	Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "MusicStorePartC", "Solution", "MusicStore"),
			//	Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "MusicStorePartD", "Solution", "MusicStore"),
			//};
			//Paths.Add(sourcePath, targetPaths);
			//SourceLabels.Add(sourcePath, new string[] { BaseCodeLabel, DomainCodeLabel });

			//sourcePath = Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "MusicStorePartB", "Solution", "MusicStore");
			//targetPaths = new string[]
			//{
			//	Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "MusicStorePartC", "Solution", "MusicStore"),
			//	Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "MusicStorePartD", "Solution", "MusicStore"),
			//};
			//Paths.Add(sourcePath, targetPaths);
			//SourceLabels.Add(sourcePath, new string[] { BaseCodeLabel, DomainCodeLabel });

			//sourcePath = Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "MusicStorePartC", "Solution", "MusicStore");
			//targetPaths = new string[]
			//{
			//	Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "MusicStorePartD", "Solution", "MusicStore"),
			//};
			//Paths.Add(sourcePath, targetPaths);
			//SourceLabels.Add(sourcePath, new string[] { BaseCodeLabel, DomainCodeLabel });
			//// End: MusicStore

			//// Project: HolidayCount
			//sourcePath = Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "TravelCount", "Solution", "TravelCount");
			//targetPaths = new string[]
			//{
			//	Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "CommunityCount", "Solution", "CommunityCount"),
			//};
			//Paths.Add(sourcePath, targetPaths);
			//SourceLabels.Add(sourcePath, new string[] { BaseCodeLabel, DomainCodeLabel });
			//// End: HolidayCount

			//// Project: QnSIdentityServer
			//sourcePath = Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "QuickNSmart", "Solution", "QuickNSmart");
			//targetPaths = new string[]
			//{
			//	Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "QnSIdentityServer", "Solution", "QnSIdentityServer"),
			//};
			//Paths.Add(sourcePath, targetPaths);
			//SourceLabels.Add(sourcePath, new string[] { QnSBaseCodeLabel });
			//// End: HolidayCount

			// Project: QuickNSmart-Projects
			var sourcePath = Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "QuickNSmart", "Solution", "QuickNSmart");
			var targetPaths = new string[]
			{
				Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "QuickNSmartTest", "Solution", "QuickNSmartTest"),
				Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "QnSIdentityServer", "Solution", "QnSIdentityServer"),
				Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "QnSTranslator", "Solution", "QnSTranslator"),
				Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "QnSHolidayCalendar", "Solution", "QnSHolidayCalendar"),
				Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "QnSCommunityCount", "Solution", "QnSCommunityCount"),
				Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "QnSContactManager", "Solution", "QnSContactManager"),
				Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "QnSMusicStore", "Solution", "QnSMusicStore"),
				//Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "QnSToDoList", "Solution", "QnSToDoList"),
				//Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "QnSTravelCount", "Solution", "QnSTravelCount"),
			};
			Paths.Add(sourcePath, targetPaths);
			SourceLabels.Add(sourcePath, new string[] { StaticLiterals.QnSBaseCodeLabel });
			// End: QuickNSmart-Projects
		}
		private static string HomePath { get; }
		private static Dictionary<string, string[]> Paths { get; set; }
		private static Dictionary<string, string[]> SourceLabels { get; set; }
		private static string[] SearchPatterns => StaticLiterals.SearchPattern.Split('|');
		private static string OldCodeCopyLabel => "@CopyCode";
		private static string CodeCopyLabel => "@CodeCopy";
		private static string DomainCodeLabel => "@DomainCode";
		private static string[] TargetLabels = new string[] { OldCodeCopyLabel, CodeCopyLabel, StaticLiterals.QnSCodeCopyLabel };


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

					foreach (var searchPattern in SearchPatterns)
					{
						var sourceCodeFiles = Solution.GetSourceCodeFiles(path.Key, searchPattern, sourceLabels);

						foreach (var targetPath in path.Value)
						{
							foreach (var sourceCodeFile in sourceCodeFiles)
							{
								Copier.CopySourceCodeFile(path.Key, sourceCodeFile, targetPath, sourceLabels, TargetLabels);
							}
						}
					}
				}
				Console.Write(queryMsg);
				input = Console.ReadLine();
			}
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
