using System;
using System.Diagnostics;
using System.IO;

namespace LargeFileSorter
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: LargeFileSorter <inputFileName> <outputFileName>");
                return;
            }
            string inputFileName = args[0];
            string outputFileName = args[1];
            if (!File.Exists(inputFileName))
            {
                Console.WriteLine($"{inputFileName} doesn't exist");
                return;
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();
            try
            {
                LargeFileSorter sorter = new LargeFileSorter();
                sorter.SortFileAsync(inputFileName, outputFileName).Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error! The file may be in wrong format or output folder is non-accessible.");
                Console.WriteLine(ex.ToString());
            }
            sw.Stop();
            Console.WriteLine($"Sorting done for {sw.ElapsedMilliseconds} ms");
        }
    }
}