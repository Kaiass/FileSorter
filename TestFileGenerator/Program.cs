using System;

namespace TestFileGenerator
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: TestFileGenerator <fileName> <fileLength>");
                return;
            }
            string fileName = args[0];
            long fileLength;
            if (!long.TryParse(args[1], out fileLength) || fileLength < 0)
            {
                Console.WriteLine("File length is incorrect.");
                return;
            }
            try
            {
                TestFileGenerator.Generate(args[0], fileLength);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error! Output folder may be non-accessible.");
                Console.WriteLine(ex.ToString());
            }
        }
    }
}