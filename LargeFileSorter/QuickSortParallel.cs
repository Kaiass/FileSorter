using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LargeFileSorter
{
    /// <summary>
    /// Parallel quick sort implementation
    /// </summary>
    internal static class QuickSortParallel
    {
        /// <summary>
        /// Synchronized quick sort
        /// </summary>
        /// <typeparam name="T">element type</typeparam>
        /// <param name="src">list of elements</param>
        /// <param name="b">start index</param>
        /// <param name="e">end index</param>
        /// <param name="comparer">element comparer</param>
        public static void QsortSync<T>(this List<T> src, int b, int e, IComparer<T> comparer = null)
        {
            if ((e - b) < 2)
                return;

            var ec = comparer ?? Comparer<T>.Default;
            T tmp;

            int i = b;
            int j = e - 1;
            T pivot_value = src[i + (j - i) / 2];
            if (ec.Compare(src[i], pivot_value) < 0)
                while (ec.Compare(src[++i], pivot_value) < 0) ;
            if (ec.Compare(src[j], pivot_value) > 0)
                while (ec.Compare(src[--j], pivot_value) > 0) ;
            while (i < j)
            {
                tmp = src[i];
                src[i] = src[j];
                src[j] = tmp;
                while (ec.Compare(src[++i], pivot_value) < 0) ;
                while (ec.Compare(src[--j], pivot_value) > 0) ;
            }
            j++;

            QsortSync(src, b, j, comparer);
            QsortSync(src, j, e, comparer);
        }

        private static void QsortParallel<T>(this List<T> src, int b, int e, IComparer<T> comparer = null)
        {
            // threshold to choose between synchronized and parallel algorithm
            const int threshold = 4096;

            if (e - b <= threshold)
            {
                QsortSync(src, b, e, comparer);
            }
            else
            {
                var ec = comparer ?? Comparer<T>.Default;
                T tmp;

                int i = b;
                int j = e - 1;
                T pivot_value = src[i + (j - i) / 2];
                if (ec.Compare(src[i], pivot_value) < 0)
                    while (ec.Compare(src[++i], pivot_value) < 0) ;
                if (ec.Compare(src[j], pivot_value) > 0)
                    while (ec.Compare(src[--j], pivot_value) > 0) ;
                while (i < j)
                {
                    tmp = src[i];
                    src[i] = src[j];
                    src[j] = tmp;
                    while (ec.Compare(src[++i], pivot_value) < 0) ;
                    while (ec.Compare(src[--j], pivot_value) > 0) ;
                }
                j++;

                // Launch sorting of sub-lists in parallel
                Parallel.Invoke(
                    () => { QsortParallel(src, b, j, comparer); },
                    () => { QsortParallel(src, j, e, comparer); }
                );
            }
        }

        /// <summary>
        /// Parallel quick sort
        /// </summary>
        /// <typeparam name="T">element type</typeparam>
        /// <param name="src">list of elements</param>
        /// <param name="comparer">element comparer</param>
        public static void QsortParallel<T>(this List<T> src, Comparer<T> comparer = null)
        {
            QsortParallel(src, 0, src.Count, comparer);
        }
    }
}
