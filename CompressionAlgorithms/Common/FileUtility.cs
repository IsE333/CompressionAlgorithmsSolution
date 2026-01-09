using System.Buffers;
using System.Collections.Concurrent;

namespace CompressionAlgorithms.Common
{
    public static class FileUtility
    {
        public static async Task CompressFile<T>(int bufferSize = 65536, string inputPath = "test.txt", string outputPath = "test_compressed.bin") where T : IAlgorithm, new()
        {
            using FileStream fsOut = new(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, FileOptions.SequentialScan);
            using FileStream fsIn = new(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.SequentialScan);
            
            IAlgorithm algorithm = new T();
            byte[] buffer = new byte[bufferSize];
            int bytesRead;
            BlockingCollection<Task<byte[]>> taskList = [];
            bool endOfWriting = false;

            _ = Task.Run(async () =>
            {
                while (!taskList.IsCompleted)
                {
                    if (taskList.TryTake(out var task))
                    {
                        var result = await task;
                        fsOut.Write(BitConverter.GetBytes((uint)result.Length), 0, 4);
                        fsOut.Write(result, 0, result.Length);
                    }
                    else
                        await Task.Delay(1);
                }
                fsOut.Write(BitConverter.GetBytes((uint)0), 0, 4); // EOF
                endOfWriting = true;
            });
            while ((bytesRead = fsIn.Read(buffer, 0, buffer.Length)) > 0)
            {
                int bytesReadTemp = bytesRead;
                byte[] temp = ArrayPool<byte>.Shared.Rent(bytesRead);
                Array.Copy(buffer, 0, temp, 0, bytesRead);

                while (taskList.Count >= Environment.ProcessorCount)
                    await Task.Delay(1);

                taskList.Add(Task.Run(() =>
                {
                    try
                    {
                        if (temp.Length > bytesReadTemp)
                            return algorithm.Compress(temp[..bytesReadTemp]);
                        return algorithm.Compress(temp);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(temp);
                    }
                }));
            }
            taskList.CompleteAdding();
            while (!endOfWriting)
                await Task.Delay(10);
            fsIn.Close();
            fsOut.Close();
        }
        public static async Task DecompressFile<T>(int bufferSize = 65536, string inputPath = "test_compressed.bin", string outputPath = "test_decompressed.txt") where T : IAlgorithm, new()
        {
            using FileStream fsOut = new(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, FileOptions.SequentialScan);
            using FileStream fsIn = new(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.SequentialScan);
            
            byte[] chunkSizeBytes = [0, 0, 0, 0];
            fsIn.ReadExactly(chunkSizeBytes, 0, 4);
            byte[] chunk = new byte[BitConverter.ToUInt32(chunkSizeBytes)];
            int bytesRead;
            BlockingCollection<Task<byte[]>> taskList = [];
            bool endOfWriting = false;

            _ = Task.Run(async () =>
            {
                while (!taskList.IsCompleted)
                {
                    if (taskList.TryTake(out var task))
                    {
                        task.Wait();
                        var result = task.Result;
                        fsOut.Write(result, 0, result.Length);
                    }
                    else
                        await Task.Delay(10);
                }
                endOfWriting = true;
            });
            while ((bytesRead = fsIn.Read(chunk, 0, chunk.Length)) > 0)
            {
                byte[] temp = new byte[chunk.Length];
                Array.Copy(chunk, 0, temp, 0, chunk.Length);
                var lzwO = new T();

                while (taskList.Count >= Environment.ProcessorCount)
                    await Task.Delay(1);

                taskList.Add(Task.Run(() =>
                {
                    return lzwO.Decompress(temp);
                }));

                fsIn.ReadExactly(chunkSizeBytes, 0, 4);
                chunk = new byte[BitConverter.ToUInt32(chunkSizeBytes)];
            }
            taskList.CompleteAdding();
            while (!endOfWriting)
                await Task.Delay(1);
            fsIn.Close();
            fsOut.Close();
        }
    }
}
