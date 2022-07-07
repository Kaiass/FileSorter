using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestFileGenerator
{
    /// <summary>
    /// Generator of file containing strings in format "Number. String"
    /// </summary>
    internal static class TestFileGenerator
    {
        private static Random rand = new Random();
        private static string chars = @"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        /// <summary>
        /// Generate the file of specified length with strings in format "Number. String"
        /// </summary>
        /// <param name="outputFileName">Output file name</param>
        /// <param name="fileLength">Desired length of file, in bytes</param>
        public static void Generate(string outputFileName, long fileLength)
        {
            // Generate "Number. String" strings where
            // Number: random positive integer (0 to System.Int32.MaxValue)
            // String: string of random characters from 'chars' (length 1 to 100)

            using (StreamWriter writer = new StreamWriter(outputFileName, false))
            {
                // save 20 strings to use them sometimes instead of generating new random string
                int saved = 20;
                List<string> savedStrings = new List<string>(saved);

                long count = 0;
                while (fileLength > 0)
                {
                    if (fileLength < 200)
                    {
                        // Fill the last line
                        int lastLength = (int)fileLength;
                        StringBuilder lastSB = new StringBuilder(lastLength);
                        lastSB.Append("1. ").Append(GenerateString(lastLength - 5));
                        writer.WriteLine(lastSB);
                        break;
                    }

                    string strPart;
                    // Each 30 iteration use some random saved string
                    if (count % 30 == 0 && savedStrings.Count > 0)
                        strPart = savedStrings[rand.Next(savedStrings.Count)];
                    else
                    {
                        int strLen = rand.Next(1, 100);
                        strPart = GenerateString(strLen);
                        if (savedStrings.Count < saved)
                            savedStrings.Add(strPart);
                    }
                    string num = rand.Next().ToString();
                    int totalLen = num.Length + 4 + strPart.Length; // 4 extra symbols are '.', ' ' and '\r', '\n'
                    StringBuilder sb = new StringBuilder(totalLen);
                    sb.Append(num).Append(". ").Append(strPart);
                    
                    writer.WriteLine(sb);

                    fileLength -= totalLen;
                    count++;
                }
            }
        }

        private static string GenerateString(int length)
        {
            if (length <= 0)
                return "0";
            StringBuilder sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                sb.Append(chars[rand.Next(chars.Length)]);
            }

            return sb.ToString();
        }
    }
}
