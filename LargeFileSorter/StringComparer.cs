using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LargeFileSorter
{
    /// <summary>
    /// Class to compare strings in format "Number. String"
    /// </summary>
    internal class StringComparer : Comparer<string>
    {
        /// <summary>
        /// Compare strings in format "Number. String"
        /// "String" parts are compared using Ordinal way (based on ASCII symbols order)
        /// "Number" parts are compared as two integers of arbitrary length without redundant leading zeroes (e.g. "001")
        /// </summary>
        /// <param name="x">First string</param>
        /// <param name="y">Second string</param>
        /// <returns>negative integer if x <![CDATA[<]]> y, positive integer if x > y, zero if x = y</returns>
        public override int Compare(string x, string y)
        {
            int ix = 0;
            //while (ix < x.Length && x[ix] != '.')
            while (x[ix] != '.')
                ix++;
            int iy = 0;
            //while (iy < y.Length && y[iy] != '.')
            while (y[iy] != '.')
                iy++;

            int px = ix;
            int py = iy;
            ix += 2;
            iy += 2;

            while (ix < x.Length && iy < y.Length)
            {
                /*if (x[ix] < y[iy])
                    return -1;
                if (x[ix] > y[iy])
                    return 1;*/
                int diff = x[ix] - y[iy];
                if (diff != 0) return diff;
                ix++;
                iy++;
            }
            if (ix >= x.Length)
            {
                if (iy < y.Length)
                    return -1;
                if (px < py)
                    return -1;
                if (px > py)
                    return 1;

                ix = 0;
                iy = 0;
                while (ix < px && iy < py)
                {
                    if (x[ix] < y[iy])
                        return -1;
                    if (x[ix] > y[iy])
                        return 1;
                    ix++;
                    iy++;
                }
                return 0;
            }
            return 1;
        }

        /// <summary>
        /// Slow but reliable comparison of strings in format "Number. String"
        /// </summary>
        /// <param name="x">First string</param>
        /// <param name="y">Second string</param>
        /// <returns>negative integer if x <![CDATA[<]]> y, positive integer if x > y, zero if x = y</returns>
        public int DefaultCompare(string x, string y)
        {
            int ix = 0;
            while (ix < x.Length && x[ix] != '.')
                ix++;
            int iy = 0;
            while (iy < y.Length && y[iy] != '.')
                iy++;

            int px = ix;
            int py = iy;
            ix += 2;
            iy += 2;

            string endx = x.Substring(ix);
            string endy = y.Substring(iy);
            int strComp = string.CompareOrdinal(endx, endy);
            if (strComp != 0)
                return strComp;

            string startx = x.Substring(0, px);
            string starty = y.Substring(0, py);
            int intx, inty;
            if (int.TryParse(startx, out intx) && int.TryParse(starty, out inty))
            {
                return intx - inty;
            }

            return 1;
        }

        /// <summary>
        /// Lightweight test of string comparer
        /// </summary>
        public static void TestCompare()
        {
            StringComparer c = new StringComparer();
            Debug.Assert(c.Compare("12312. asd", "12312. as") > 0);
            Debug.Assert(c.Compare("12312. asd", "1231. asd") > 0);
            Debug.Assert(c.Compare("12311. asd", "12312. asd") < 0);
            Debug.Assert(c.Compare("12312. asd", "12312. asd") == 0);
            Debug.Assert(c.Compare("12312. Z", "12312. z") < 0);
        }
    }
}
