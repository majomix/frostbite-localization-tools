using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FrostbiteFileSystemTools.Model
{
    internal class BundleBinaryWriter : BinaryWriter
    {
        public BundleBinaryWriter(Stream stream)
            : base(stream) { }

        public void WriteNullTerminatedStringWithLEB128Prefix(string value)
        {
            Write7BitEncodedInt(value.Length + 1);
            WriteNullTerminatedString(value);
        }

        public void WriteNullTerminatedString(string value)
        {
            value = value + '\0';
            Write(Encoding.ASCII.GetBytes(value));
        }

        public void Write(FrostbiteHeader header)
        {
            if (header == null) return;

            Write(header.Signature);
            Write(header.Version);
            Write((Int32)0);

            if (header.Version == 0x01)
            {
                Write(header.XorKey);
            }
            Write(Encoding.ASCII.GetBytes(header.HexValue));

            if (header.Version != 0x01)
            {
                Write(new byte[256]);
            }
            Write(new byte[34]);
        }

        public void Write(SuperBundle bundle)
        {
            if (bundle == null) return;

            Write(bundle.InitialByte);
            Write7BitEncodedInt(bundle.Limit);

            foreach(BundleList bundleList in bundle.BundleCollections)
            {
                Write(bundleList);
            }

            foreach(var property in bundle.Properties)
            {
                object value = property.Value.Value;
                Write(property.Value.Opcode);
                WriteNullTerminatedString(property.Key);
                
                switch (property.Value.Opcode)
                {
                    // boolean
                    case 0x06:
                        Write((bool)value ? (byte)1 : (byte)0);
                        break;
                    // string
                    case 0x07:
                        WriteNullTerminatedStringWithLEB128Prefix((string)value);
                        break;
                    // 4-byte number
                    case 0x08:
                        Write((Int32)value);
                        break;
                    // 8-byte number
                    case 0x09:
                        Write((Int64)value);
                        break;
                    // 16 or 20 bytes hash
                    case 0x0f:
                    case 0x10:
                        Write((byte[])value);
                        break;
                    // variable size blob
                    case 0x02:
                    case 0x13:
                        Write7BitEncodedInt(((byte[])value).Length);
                        Write((byte[])value);
                        break;
                    default:
                        throw new InvalidDataException("Unknown opcode.");
                }
            }

            Write((byte)0);
        }

        public void Write(BundleList bundleList)
        {
            if (bundleList == null) return;

            Write(bundleList.Opcode);
            WriteNullTerminatedString(bundleList.Name);
            Write7BitEncodedInt(bundleList.Limit);

            foreach (SuperBundle bundle in bundleList.Bundles)
            {
                Write(bundle);
            }

            Write((byte)0);
        }

        public void Write(byte version, Catalogue catalogue)
        {
            if(version == 0x01)
            {
                Write(catalogue.Header);
            }
            
            Write(Encoding.ASCII.GetBytes(catalogue.Signature));

            if (version == 0x01)
            {
                Write(catalogue.NumberOfFiles);
                Write(catalogue.NumberOfHashes);
                if(catalogue.Extra != null)
                {
                    Write(catalogue.Extra);
                }
            }

            foreach(var entry in catalogue.Files)
            {
                Write(version, entry);
            }

            foreach(byte[] hash in catalogue.Hashes)
            {
                Write(hash);
            }
        }

        public void Write(byte version, KeyValuePair<byte[], CatalogueEntry> entry)
        {
            Write(entry.Key);
            Write(entry.Value.Offset);
            Write(entry.Value.Size);
            if(version == 0x01)
            {
                Write(entry.Value.Extra);
            }
            Write(entry.Value.Archive);
        }
    }
}
