namespace CompressionAlgorithms
{
    public interface IAlgorithm
    {
        byte[] Compress(byte[] data);
        byte[] Decompress(byte[] compressedData);
    }
}
