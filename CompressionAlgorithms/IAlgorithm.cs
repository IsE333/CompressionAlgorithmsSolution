namespace CompressionAlgorithms
{
    public interface IAlgorithm
    {
        string AlgorithmName { get; }
        byte[] Compress(byte[] data, int dataSize);
        byte[] Decompress(byte[] compressedData);
    }
}
