using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CompressionAlgorithms.Common
{
    public static class FileUtility
    {
        public static async Task<long[]> CompressFile(Func<byte[], int, byte[]> compressMethod, IProgress<int> progress, int bufferSize = 65536, string inputPath = "test.txt", string outputPath = "test_compressed.bin")
        {
            var channelOptions = new BoundedChannelOptions(Environment.ProcessorCount*2)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleWriter = true,
                SingleReader = true
            };
            var channel = Channel.CreateBounded<Task<byte[]>>(channelOptions);

            using FileStream fsOut = new(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, FileOptions.SequentialScan);
            using FileStream fsIn = new(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.SequentialScan);
            
            byte[] buffer = new byte[bufferSize];
            int bytesRead;

            var consumerTask = Task.Run(async () =>
            {
                await foreach (var task in channel.Reader.ReadAllAsync())
                {
                    var result = await task;
                    await fsOut.WriteAsync(BitConverter.GetBytes((uint)result.Length), 0, 4);
                    await fsOut.WriteAsync(result, 0, result.Length);
                }
                await fsOut.WriteAsync(BitConverter.GetBytes((uint)0), 0, 4); // EOF
            });
            while ((bytesRead = await fsIn.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                byte[] temp = ArrayPool<byte>.Shared.Rent(bytesRead);
                int localLength = bytesRead;
                Array.Copy(buffer, 0, temp, 0, bytesRead);
                
                var task = Task.Run(() =>
                {
                    try
                    {
                        return compressMethod(temp, localLength);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(temp);
                    }
                });
                progress?.Report((int)((100 * fsIn.Position) / fsIn.Length));
                await channel.Writer.WriteAsync(task);
            }
            channel.Writer.Complete();
            await consumerTask;
            return [fsIn.Length, fsOut.Length];
        }
        public static async Task DecompressFile(Func<byte[], byte[]> decompressMethod, IProgress<int> progress, int bufferSize = 65536, string inputPath = "test_compressed.bin", string outputPath = "test_decompressed.txt")
        {
            var channelOptions = new BoundedChannelOptions(Environment.ProcessorCount * 2)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleWriter = true,
                SingleReader = true
            };
            var channel = Channel.CreateBounded<Task<byte[]>>(channelOptions);

            using FileStream fsOut = new(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, FileOptions.SequentialScan);
            using FileStream fsIn = new(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.SequentialScan);
            
            byte[] chunkSizeBytes = [0, 0, 0, 0];
            fsIn.ReadExactly(chunkSizeBytes, 0, 4);
            byte[] chunk = new byte[BitConverter.ToUInt32(chunkSizeBytes)];
            int bytesRead;

            var consumerTask = Task.Run(async () =>
            {
                await foreach (var task in channel.Reader.ReadAllAsync())
                {
                    var result = await task;
                    await fsOut.WriteAsync(result, 0, result.Length);
                }
            });
            while ((bytesRead = await fsIn.ReadAsync(chunk, 0, chunk.Length)) > 0)
            {
                byte[] temp = new byte[chunk.Length];
                Array.Copy(chunk, 0, temp, 0, chunk.Length);

                var task = Task.Run(() =>
                {
                    return decompressMethod(temp);
                });
                progress?.Report((int)((100 * fsIn.Position) / fsIn.Length));
                await channel.Writer.WriteAsync(task);

                await fsIn.ReadExactlyAsync(chunkSizeBytes, 0, 4);
                chunk = new byte[BitConverter.ToUInt32(chunkSizeBytes)];
            }
            channel.Writer.Complete();
            await consumerTask;
        }
    }
}
