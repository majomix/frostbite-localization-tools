using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostbiteFileSystemTools.Model
{
    public class Catalogue
    {
        private readonly string allowedSignature = @"NyanNyanNyanNyan";

        private string mySignature;

        public Catalogue(string path, Dictionary<byte[], CatalogueEntry> dictionary, List<byte[]> hashes)
        {
            Path = path;
            Files = dictionary;
            Hashes = hashes;
        }

        public string Signature
        {
            get { return mySignature; }
            set
            {
                if (allowedSignature != value)
                {
                    throw new InvalidDataException();
                }
                else
                {
                    mySignature = value;
                }
            }
        }

        public FrostbiteHeader Header { get; set; }
        public int EntrySize { get; set; }
        public Dictionary<byte[], CatalogueEntry> Files { get; private set; }
        public IEnumerable<byte[]> Hashes { get; private set; }
        public int NumberOfFiles { get; set; }
        public int NumberOfHashes { get; set; }
        public byte[] Extra { get; set; }
        public string Path { get; private set; }
    }

    public class CatalogueEntry
    {
        public int Size { get; set; }
        public int Offset { get; set; }
        public int Archive { get; set; }
        public int Extra { get; set; }
        public Catalogue Parent { get; set; }
        public string ResolvedName { get; set; }
        public bool Changed { get; set; }
    }
}
