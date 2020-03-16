using System;

namespace SolutionCopier.ConApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Solution copier!");

            string sourcePath = @"C:\Users\ggehrer\Google Drive\Schule\CSharp\QuickNSmart\Solution\QuickNSmart";
            string targetPath = @"C:\Users\ggehrer\Google Drive\Schule\CSharp\QnSInvoiceSystem\Solution\QnSInvoiceSystem";

            var sc = new SolutionCopier();

            sc.Copy(sourcePath, targetPath);
        }
    }
}
