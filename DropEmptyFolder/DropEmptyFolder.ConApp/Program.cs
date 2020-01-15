using System;
using System.IO;
using System.Linq;

namespace DropEmptyFolder.ConApp
{
	class Program
	{
		private static void Main(string[] args)
		{
			PrintHeader();

			if (args.Length == 1)
			{
				if (Directory.Exists(args[0]))
				{
					DropEmptyFolder(args[0]);
				}
				else
				{
					Console.WriteLine($"Directory {args[0]} not found!");
				}
			}
			else
			{
				Console.WriteLine("DropEmptyFolder folderpath");
			}
		}

		private static void DropEmptyFolder(string directory)
		{
			var di = new DirectoryInfo(directory);
			var hasFiles = di.GetFiles("*.*", SearchOption.AllDirectories).Any();

			if (hasFiles == false)
			{
				try
				{
					Directory.Delete(directory);
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine(ex.Message);
				}
			}
			else
			{
				foreach (var item in di.GetDirectories())
				{
					DropEmptyFolder(item.FullName);
				}
			}
		}
		static void PrintHeader()
		{
			Console.Clear();
			Console.SetCursorPosition(0, 0);
			Console.WriteLine($"{nameof(DropEmptyFolder)}:");
			Console.WriteLine("==========================================");
			Console.WriteLine();
		}
	}
}
