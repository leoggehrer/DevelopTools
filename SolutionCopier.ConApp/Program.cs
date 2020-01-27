using System;

namespace SolutionCopier.ConApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Solution copier!");

            string sourcePath = @"C:\Users\g.gehrer\Google Drive\Schule\CSharp\QuickNSmart";
            string targetPath = @"C:\Users\g.gehrer\Google Drive\Schule\CSharp\QnSIdentityServer";

            var sc = new SolutionCopier();

            sc.Copy(sourcePath, targetPath);
        }
    }
}
