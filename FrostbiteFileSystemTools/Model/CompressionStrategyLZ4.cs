using System.Runtime.InteropServices;

namespace FrostbiteFileSystemTools.Model
{
    internal class CompressionStrategyLZ4 : ICompressionStrategy
    {
        public byte CompressionSignature
        {
            get { return 0x09; }
        }

        public int Compress(byte[] inBuffer, byte[] outBuffer, int uncompressedSize)
        {
            return LZ4Handler.LZ4_compress(inBuffer, outBuffer, uncompressedSize);
        }
    }

    public static class LZ4Handler
    {
        [DllImport("lz4.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int LZ4_compress(byte[] source, byte[] dest, int sourceSize);

        [DllImport("lz4.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int LZ4_decompress_safe(byte[] source, byte[] dest, int compressedSize, int maxDecompressedSize);
    }
}
