using DevelopCommon.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DevelopCommon
{
    public static class Copier
    {
		public static bool SynchronizeSourceCodeFile(string sourcePath, string sourceFilePath, string targetPath, string[] sourceLabels, string[] targetLabels)
		{
			sourceLabels.CheckArgument(nameof(sourceLabels));
			targetLabels.CheckArgument(nameof(targetLabels));

			bool result = false;
			bool canCopy = true;
			string sourceFileName = Path.GetFileNameWithoutExtension(sourceFilePath);
			string sourceSolutionName = Solution.GetSolutionNameFromPath(sourcePath);
			string targetSolutionName = Solution.GetSolutionNameFromPath(targetPath);
			string sourceProjectName = Solution.GetProjectNameFromFilePath(sourceFilePath, sourceSolutionName);
			string targetProjectName = sourceProjectName.Replace(sourceSolutionName, targetSolutionName);
			string targetFilePath = sourceFilePath.Replace(sourcePath, targetPath).Replace(sourceProjectName, targetProjectName).Replace(sourceSolutionName, targetSolutionName);
			string targetFileFolder = Path.GetDirectoryName(targetFilePath);

			if (Directory.Exists(targetFileFolder) == false)
			{
				Directory.CreateDirectory(targetFileFolder);
			}
			if (File.Exists(targetFilePath))
			{
				var lines = File.ReadAllLines(targetFilePath, Encoding.Default);

				canCopy = false;
				if (lines.Any() && targetLabels.Any(l => lines.First().Contains(l)))
				{
					canCopy = true;
				}
			}
			if (canCopy)
			{
				var cpyLines = new List<string>();
				var srcLines = File.ReadAllLines(sourceFilePath, Encoding.Default)
								   .Select(i => i.Replace(sourceSolutionName, targetSolutionName));
				var srcFirst = srcLines.FirstOrDefault();

				if (srcFirst != null)
				{
					var label = sourceLabels.FirstOrDefault(l => srcFirst.Contains(l));

					cpyLines.Add(srcFirst.Replace(label ?? string.Empty, StaticLiterals.QnSCodeCopyLabel));
				}
				cpyLines.AddRange(File.ReadAllLines(sourceFilePath, Encoding.Default)
								   .Skip(1)
								   .Select(i => i.Replace(sourceSolutionName, targetSolutionName)));
				File.WriteAllLines(targetFilePath, cpyLines.ToArray(), Encoding.Default);
			}
			return result;
		}
		public static bool CopySourceCodeFile(string sourcePath, string sourceFilePath, string targetPath, string[] sourceLabels, string[] targetLabels)
		{
			sourceLabels.CheckArgument(nameof(sourceLabels));
			targetLabels.CheckArgument(nameof(targetLabels));

			bool result = false;
			bool canCopy = true;
			string sourceFileName = Path.GetFileNameWithoutExtension(sourceFilePath);
			string sourceSolutionName = Solution.GetSolutionNameFromPath(sourcePath);
			string targetSolutionName = Solution.GetSolutionNameFromPath(targetPath);
			string sourceProjectName = Solution.GetProjectNameFromFilePath(sourceFilePath, sourceSolutionName);
			string targetProjectName = sourceProjectName.Replace(sourceSolutionName, targetSolutionName);
			string targetFilePath = sourceFilePath.Replace(sourcePath, targetPath).Replace(sourceProjectName, targetProjectName);
			string targetFileFolder = Path.GetDirectoryName(targetFilePath);

			if (Directory.Exists(targetFileFolder) == false)
			{
				Directory.CreateDirectory(targetFileFolder);
			}
			if (File.Exists(targetFilePath))
			{
				var lines = File.ReadAllLines(targetFilePath, Encoding.Default);

				canCopy = false;
				if (lines.Any() && targetLabels.Any(l => lines.First().Contains(l)))
				{
					canCopy = true;
				}
			}
			if (canCopy)
			{
				var cpyLines = new List<string>();
				var srcLines = File.ReadAllLines(sourceFilePath, Encoding.Default)
								   .Select(i => i.Replace(sourceSolutionName, targetSolutionName));
				var srcFirst = srcLines.FirstOrDefault();

				if (srcFirst != null)
				{
					var label = sourceLabels.FirstOrDefault(l => srcFirst.Contains(l));

					cpyLines.Add(srcFirst.Replace(label ?? string.Empty, StaticLiterals.QnSCodeCopyLabel));
				}
				cpyLines.AddRange(File.ReadAllLines(sourceFilePath, Encoding.Default)
								   .Skip(1)
								   .Select(i => i.Replace(sourceSolutionName, targetSolutionName)));
				File.WriteAllLines(targetFilePath, cpyLines.ToArray(), Encoding.Default);
			}
			return result;
		}
	}
}
