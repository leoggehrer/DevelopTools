using System;

namespace QnSSolutionCopier.ConApp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Solution copier!");

            string sourcePath = @"C:\Users\g.gehrer\Google Drive\QnSDevelop\QuickNSmartForBusiness\Solution\QuickNSmart";
            string targetPath = @"C:\Users\g.gehrer\Google Drive\QnSDevelop\QnSHungryLama\Solution\QnSHungryLama";

            var sc = new SolutionCopier();

            sc.Copy(sourcePath, targetPath);
        }
    }
}
