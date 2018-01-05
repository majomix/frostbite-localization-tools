namespace FrostbiteFileSystemTools.Model
{
    interface ICompressionStrategy
    {
        byte CompressionSignature { get; }
        int Compress(byte[] inBuffer, byte[] outBuffer, int uncompressedSize);
    }
}
