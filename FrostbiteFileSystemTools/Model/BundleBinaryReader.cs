using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System;

namespace FrostbiteFileSystemTools.Model
{
    internal class BundleBinaryReader : BinaryReader
    {
        public BundleBinaryReader(FileStream fileStream)
            : base(fileStream) { }

        public override string ReadString()
        {
            int length = Read7BitEncodedInt();
            return new string(ReadChars(length)).TrimEnd('\0');
        }

        public string ReadNullTerminatedString()
        {
            List<byte> stringBytes = new List<byte>();
            int currentByte;

            while ((currentByte = ReadByte()) != 0x00)
            {
                stringBytes.Add((byte)currentByte);
            }

            return Encoding.ASCII.GetString(stringBytes.ToArray());
        }

        public FrostbiteHeader ReadTableOfContentsHeader()
        {
            FrostbiteHeader header = new FrostbiteHeader();

            header.Signature = ReadBytes(3);
            header.Version = ReadByte();

            ReadInt32();

            if (header.Version == 0x01)
            {
                header.XorKey = ReadBytes(256);
            }
            header.HexValue = new string(ReadChars(258));

            if (header.Version != 0x01)
            {
                ReadBytes(256);
            }

            ReadBytes(34);

            return header;
        }

        public SuperBundle ReadTableOfContentsPayload()
        {
            SuperBundle superBundle = new SuperBundle();
            List<BundleList> bundleList = new List<BundleList>();
            Dictionary<string, PropertyValue> properties = new Dictionary<string, PropertyValue>();

            superBundle.Properties = properties;
            superBundle.BundleCollections = bundleList;

            byte currentInitialByte = ReadByte();
            if (currentInitialByte == 0)
            {
                return null;
            }

            superBundle.InitialByte = currentInitialByte;
            superBundle.Limit = Read7BitEncodedInt();
            long startPosition = BaseStream.Position;

            while (BaseStream.Position < startPosition + superBundle.Limit)
            {
                byte entryOpCode = ReadByte();

                if(entryOpCode == 0)
                {
                    return superBundle;
                }

                string entryName = ReadNullTerminatedString();
                object value;

                switch (entryOpCode)
                {
                    // list
                    case 0x01:
                        bundleList.Add(ReadBundleList(entryName));
                        continue;
                    // boolean
                    case 0x06:
                        value = ReadByte() == 0x01;
                        break;
                    // string
                    case 0x07:
                        value = ReadString();
                        break;
                    // 4-byte number
                    case 0x08:
                        value = ReadInt32();
                        break;
                    // 8-byte number
                    case 0x09:
                        value = ReadInt64();
                        break;
                    // 16 bytes hash
                    case 0x0f:
                        value = ReadBytes(16);
                        break;
                    // 20 bytes hash
                    case 0x10:
                        value = ReadBytes(20);
                        break;
                    // variable size blob
                    case 0x02:
                    case 0x13:
                        int blobSize = Read7BitEncodedInt();
                        value = ReadBytes(blobSize);
                        break;
                    default:
                        throw new InvalidDataException("Unknown opcode.");
                }

                properties.Add(entryName, new PropertyValue(entryOpCode, value));
            }

            return superBundle;
        }

        public BundleList ReadBundleList(string listName)
        {
            List<SuperBundle> listOfBundles = new List<SuperBundle>();

            string[] compare = new string[] { "bundles", "chunks", "ebx", "res", "chunkMeta", "dbx" };

            if (!compare.Contains(listName))
            {
                throw new InvalidDataException("Unknown list type.");
            }

            int limit = Read7BitEncodedInt();

            long startPosition = BaseStream.Position;

            while (BaseStream.Position < startPosition + limit)
            {
                SuperBundle currentBundle = ReadTableOfContentsPayload();
                if (currentBundle != null)
                {
                    listOfBundles.Add(currentBundle);
                }
            }

            BundleList bundleList = new BundleList();
            bundleList.Name = listName;
            bundleList.Bundles = listOfBundles;
            bundleList.Limit = limit;

            return bundleList;
        }

        public Catalogue ReadCatalogue(byte version, string path)
        {
            Dictionary<byte[], CatalogueEntry> dictionary = new Dictionary<byte[], CatalogueEntry>(new StructuralEqualityComparer());
            List<byte[]> hashes = new List<byte[]>();
            Catalogue catalogue = new Catalogue(path, dictionary, hashes);

            if(version == 0x01)
            {
                catalogue.Header = ReadTableOfContentsHeader();
            }

            catalogue.Signature = new string(ReadChars(16));

            if (version == 0x01)
            {
                catalogue.NumberOfFiles = ReadInt32();
                catalogue.NumberOfHashes = ReadInt32();

                byte[] extraBytes = ReadBytes(16);
                if(extraBytes[15] == 0)
                {
                    catalogue.Extra = extraBytes;
                }
                else
                {
                    BaseStream.Seek(-16, SeekOrigin.Current);
                }

                for (int i = 0; i < catalogue.NumberOfFiles; i++)
                {
                    BuildCatalogueEntry(catalogue, version);
                }

                for (int i = 0; i < catalogue.NumberOfHashes; i++)
                {
                    hashes.Add(ReadBytes(60));
                }
            }
            else
            {
                while(BaseStream.Position < BaseStream.Length)
                {
                    BuildCatalogueEntry(catalogue, version);
                }
            }

            return catalogue;
        }

        private void BuildCatalogueEntry(Catalogue catalogue, byte version)
        {
            byte[] hash = ReadBytes(20);
            CatalogueEntry entry = new CatalogueEntry();
            entry.Offset = ReadInt32();
            entry.Size = ReadInt32();

            if (version == 0x01)
            {
                entry.Extra = ReadInt32();
            }

            entry.Archive = ReadInt32();

            catalogue.Files[hash] = entry;
            entry.Parent = catalogue;
        }
    }
}
