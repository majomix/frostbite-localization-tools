using ZstdNet;

namespace FrostbiteFileSystemTools.Model
{
    internal class CompressionStrategyZstd : ICompressionStrategy
    {
        public byte CompressionSignature
        {
            get { return 0x0F; }
        }

        public int Compress(byte[] inBuffer, byte[] outBuffer, int uncompressedSize)
        {
            using (CompressionOptions options = new CompressionOptions(CompressionOptions.MaxCompressionLevel))
            using (Compressor compressor = new Compressor(options))
            {
                return compressor.Wrap(inBuffer, outBuffer, 0);
            }
        }
    }

}
