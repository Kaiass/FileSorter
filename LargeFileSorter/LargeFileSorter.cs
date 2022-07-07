using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LargeFileSorter
{
    /// <summary>
    /// Class for sorting large files containing strings in format "Number. String"
    /// </summary>
    internal class LargeFileSorter
    {
        const long GIGABYTE = 1024 * 1024 * 1024;
        const long MEGABYTE = 1024 * 1024;
        const int BufferSize = 4194304;
        const int ListCapacity = 20000000;
        const int ChunkCoefficient = 5;
        const int MemoryChunks = 4;

        string _inputFileName;
        string _outputFileName;
        long _fileLength;
        long _numChunks;
        int _numMemChunks;
        string _folderName;

        long _chunkSize;
        static readonly StringComparer sc = new StringComparer();

        public LargeFileSorter()
        {
        }

        /// <summary>
        /// Asynchronously sort file
        /// </summary>
        /// <param name="inputFileName">Input file name</param>
        /// <param name="outputFileName">Output file name</param>
        public async Task SortFileAsync(string inputFileName, string outputFileName)
        {
            _inputFileName = inputFileName;
            _outputFileName = outputFileName;
            
            // Will use output file directory as a location for temporary files (chunks)
            _folderName = Path.GetDirectoryName(_outputFileName);
            _fileLength = new FileInfo(_inputFileName).Length;

            // Configure size and number of file chunks and in-memory chunks
            ConfigureChunks();

            // Basic algorithm:
            // 1. Split input file to file chunks if RAM is not enough
            // 2. Sort each chunk in memory one by one (parallel to reading next chunk):
            //      a) Each chunk is split into in-memory chunks
            //      b) All in-memory chunks are sorted in parallel
            //         Each in-memory chunk sorting is launched immediately after its reading
            //         Parallel quick sort is used for sorting
            //      c) Merge in-memory chunks (if more than one)
            // 3. Merge chunks (if more than one) to the output file
            await SplitFileAndSortChunks();
            if (_numChunks > 1)
                MergeChunks();
        }

        private void ConfigureChunks()
        {
            if (_fileLength > 4 * GIGABYTE)
            {
                if (PerformanceCounterCategory.Exists("Memory"))
                {
                    // For very large files get the amount of available RAM
                    // Each chunk requires 'ChunkCoefficient' of RAM, e.g. 1GB chunk requires 5GB RAM
                    var netAvailableMemoryCounter = new PerformanceCounter("Memory", "Available Bytes");
                    _chunkSize = netAvailableMemoryCounter.RawValue / ChunkCoefficient;
                }
                else
                    // Use 2GB chunk size by default
                    _chunkSize = 2 * GIGABYTE;

                _numChunks = _fileLength / _chunkSize;
                if (_numChunks == 0)
                    _numChunks = 1;
            }
            else
            {
                // For not very large files use 1 chunk
                _numChunks = 1;
            }

            // Use 4 memory chunks for not very small file
            if (_fileLength < MEGABYTE)
                _numMemChunks = 1;
            else
                _numMemChunks = MemoryChunks;
        }

        private async Task SplitFileAndSortChunks()
        {
            if (_numChunks == 1)
            {
                await SortChunkAsync(_inputFileName, _outputFileName);
                return;
            }

            // Split file into '_numChunks' chunks of equal length, e.g. "0.chk", "1.chk", "2.chk", etc.
            // Start sorting each chunk immediately when chunk is fully read
            // While sorting continue writing next chunk (because it doesn't take much RAM)
            long chunkSize = _fileLength / _numChunks;
            int currentChunk = 0;
            long currentChunkSize = 0;
            string chunkName = Path.Combine(_folderName, $"{currentChunk}.chk");
            Task lastTask = null;
            StreamWriter writer = new StreamWriter(chunkName, false);
            using (StreamReader reader = new StreamReader(_inputFileName))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    writer.WriteLine(line);
                    currentChunkSize += line.Length + 2;
                    if (currentChunkSize >= chunkSize)
                    {
                        writer.Dispose();
                        // Start sorting and continue writing next chunk
                        // Wait only for the previous chunk sorting
                        if (lastTask != null)
                            await lastTask;
                        lastTask = SortChunkAsync(chunkName, chunkName);

                        currentChunk++;
                        currentChunkSize = 0;

                        chunkName = Path.Combine(_folderName, $"{currentChunk}.chk");
                        writer = new StreamWriter(chunkName, false);
                    }
                }
            }
            writer.Dispose();
            await SortChunkAsync(chunkName, chunkName);
        }

        private async Task SortChunkAsync(string inputFileName, string outputFileName)
        {
            // Split chunk into '_numMemChunks' in-memory chunks of equal length
            // Start sorting each in-memory chunk immediately when it is fully read
            // All operations are running in parallel
            // Sorting is done using parallel quick sort
            // Wait for all sortings completed
            // Merge the sorted in-memory chunks (if more than one)
            long memChunkSize = new FileInfo(inputFileName).Length / _numMemChunks;
            int currentMemChunk = 0;
            long currentMemChunkSize = 0;
            List<string>[] memChunks = new List<string>[_numMemChunks];
            memChunks[currentMemChunk] = new List<string>(ListCapacity / _numMemChunks); // ***
            Task[] sortMemChunk = new Task[_numMemChunks];
            using (StreamReader reader = new StreamReader(inputFileName, Encoding.ASCII, false, BufferSize))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    memChunks[currentMemChunk].Add(line);
                    currentMemChunkSize += line.Length + 2;
                    if (currentMemChunkSize >= memChunkSize)
                    {
                        int ch = currentMemChunk;
                        sortMemChunk[currentMemChunk] = new Task(() => { memChunks[ch].QsortParallel(sc); });
                        sortMemChunk[currentMemChunk].Start();

                        currentMemChunk++;
                        currentMemChunkSize = 0;
                        if (currentMemChunk < _numMemChunks)
                            memChunks[currentMemChunk] = new List<string>(ListCapacity / _numMemChunks);
                    }
                }
            }
            if (currentMemChunk < _numMemChunks)
            {
                sortMemChunk[currentMemChunk] = new Task(() => memChunks[currentMemChunk].QsortParallel(sc));
                sortMemChunk[currentMemChunk].Start();
            }

            await Task.WhenAll(sortMemChunk);

            if (_numMemChunks > 1)
            {
                MergeMemChunks(memChunks, outputFileName);
            }
            else
            {
                using (StreamWriter writer = new StreamWriter(outputFileName, false, Encoding.ASCII, BufferSize))
                {
                    foreach (string line in memChunks[0])
                        writer.WriteLine(line);
                }
            }
        }

        private void MergeMemChunks(List<string>[] lists, string outFileName)
        {
            // Merge in-memory chunks
            // On each iteration simple 'for' loop is used to find the minimum element in all lists
            int[] indices = new int[_numMemChunks];
            using (StreamWriter writer = new StreamWriter(outFileName, false, Encoding.ASCII, BufferSize))
            {
                while (true)
                {
                    string min = null;
                    int minIndex = -1;
                    for (int i = 0; i < _numMemChunks; i++)
                    {
                        if (indices[i] < lists[i].Count && (min == null || sc.Compare(lists[i][indices[i]], min) < 0))
                        {
                            min = lists[i][indices[i]];
                            minIndex = i;
                        }
                    }
                    if (min == null)
                        break;
                    writer.WriteLine(min);
                    indices[minIndex]++;
                }
            }
        }

        private void MergeChunks()
        {
            // Merge chunks
            // On each iteration MinHeap is used to find the minimum element in all chunks
            using (StreamWriter writer = new StreamWriter(_outputFileName, false, Encoding.ASCII, BufferSize))
            {
                StreamReader[] readers = new StreamReader[_numChunks];
                IList<Node> list = new List<Node>();
                for (int i = 0; i < _numChunks; i++)
                {
                    string chunkName = Path.Combine(_folderName, $"{i}.chk");
                    readers[i] = new StreamReader(chunkName, Encoding.ASCII, false, BufferSize);
                    string line = readers[i].ReadLine();
                    list.Add(new Node() { Index = i, Value = line });
                }

                var minHeap = new MinHeap(new StringComparer());
                minHeap.Heapify(list);

                while (minHeap.Count > 0)
                {
                    var node = minHeap.Extract();
                    writer.WriteLine(node.Value);
                    string line = readers[node.Index].ReadLine();
                    if (line != null) minHeap.Insert(new Node() { Index = node.Index, Value = line });
                }

                // Clean up chunks
                for (int i = 0; i < _numChunks; i++)
                {
                    readers[i].Dispose();
                    File.Delete(Path.Combine(_folderName, $"{i}.chk"));
                }
            }
        }

        /// <summary>
        /// Validates the order of strings in file
        /// </summary>
        /// <param name="fileName">file name</param>
        /// <returns>true if strings in file are sorted, otherwise false</returns>
        public static bool Validate(string fileName)
        {
            using (StreamReader reader = new StreamReader(fileName, Encoding.ASCII, false, BufferSize))
            {
                string line;
                string prevLine = null;
                while ((line = reader.ReadLine()) != null)
                {
                    // Use slow but the most reliable string comparison
                    if (prevLine != null && sc.DefaultCompare(prevLine, line) > 0)
                    {
                        Console.WriteLine($"ERROR! line {prevLine} is larger than {line}");
                        return false;
                    }
                    prevLine = line;
                }
            }

            return true;
        }

        #region Not used implementations
        // Not used because currently the number of in-memory chunks is small
        private void MergeMemChunksUsingHeap(List<string>[] lists, string outFileName)
        {
            int[] pos = new int[lists.Length];

            var minHeap = new MinHeap(new StringComparer());
            minHeap.Heapify(lists);
            using (StreamWriter writer = new StreamWriter(outFileName, false, Encoding.ASCII, BufferSize))
            {
                while (minHeap.Count > 0)
                {
                    var node = minHeap.Extract();
                    writer.WriteLine(node.Value);
                    pos[node.Index]++;
                    if (pos[node.Index] < lists[node.Index].Count) minHeap.Insert(new Node() { Index = node.Index, Value = lists[node.Index][pos[node.Index]] });
                }
            }
        }

        // Not used
        // Simple merge of 2 sorted lists
        private void Merge2MemChunks(List<string>[] lists, string outFileName)
        {
            List<string> list1 = lists[0];
            List<string> list2 = lists[1];
            int i1 = 0;
            int i2 = 0;
            var sc = new StringComparer();
            using (StreamWriter writer = new StreamWriter(outFileName, false, Encoding.ASCII, BufferSize))
            {
                while (i1 < list1.Count || i2 < list2.Count)
                {
                    if (i2 >= list2.Count)
                    {
                        writer.WriteLine(list1[i1]);
                        i1++;
                    }
                    else if (i1 >= list1.Count)
                    {
                        writer.WriteLine(list2[i2]);
                        i2++;
                    }
                    else if (sc.Compare(list1[i1], list2[i2]) < 0)
                    {
                        writer.WriteLine(list1[i1]);
                        i1++;
                    }
                    else
                    {
                        writer.WriteLine(list2[i2]);
                        i2++;
                    }
                }
            }
        }
        #endregion
    }
}
