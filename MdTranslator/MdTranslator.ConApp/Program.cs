using System;
using System.IO;

namespace MdTranslator.ConApp
{
	class Program
	{
		static Program()
		{
			HomePath = (Environment.OSVersion.Platform == PlatformID.Unix ||
						Environment.OSVersion.Platform == PlatformID.MacOSX)
					   ? Environment.GetEnvironmentVariable("HOME")
					   : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
			BasePaths = new string[]
			{
				Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "MusicStorePartA", "Solution", "MusicStore"),
				Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "MusicStorePartB", "Solution", "MusicStore"),
				Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "MusicStorePartC", "Solution", "MusicStore"),
				Path.Combine(HomePath, "Google Drive", "Schule", "CSharp", "MusicStorePartD", "Solution", "MusicStore"),
			};
		}

		private static string HomePath { get; }
		private static string[] BasePaths { get; set; }
		private static string MdSourceExt => "*.md";

		static void Main(string[] args)
		{
			string input;
			string queryMsg = "Translate [y|n]?: ";

			PrintHeader();
			Console.Write(queryMsg);
			input = Console.ReadLine();
			while (input.Equals("y", StringComparison.CurrentCultureIgnoreCase))
			{
				Console.Clear();
				PrintHeader();
				foreach (var basePath in BasePaths)
				{
					foreach (var filePath in Directory.GetFiles(basePath, MdSourceExt, SearchOption.AllDirectories))
					{
						Console.WriteLine($"Markdown-Translator translate: {filePath}...");

						MdTranslator.ReplaceDocumenTags(filePath, basePath);
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
			Console.WriteLine($"{nameof(MdTranslator)}:");
			Console.WriteLine("==========================================");
			Console.WriteLine();
			foreach (var basePath in BasePaths)
			{
				Console.WriteLine($"{nameof(basePath)}:  {basePath}");
			}
			Console.WriteLine();
		}
	}
}
