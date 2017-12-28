﻿using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FrostbiteFileSystemTools.Model
{
    public class TableOfContents
    {
        public FrostbiteHeader Header { get; set; }
        public SuperBundle Payload { get; set; }
    }

    public class FrostbiteHeader
    {
        private readonly byte[] allowedSignature = { 0x0, 0xD1, 0xCE };
        private readonly IEnumerable<byte> allowedVersions = new byte[] { 0x01, 0x03 };
        private readonly string allowedHexValue = @"xa37dd45ffe100bfffcc9753aabac325f07cb3fa231144fe2e33ae4783feead2b8a73ff021fac326df0ef9753ab9cdf6573ddff0312fab0b0ff39779eaff312a4f5de65892ffee33a44569bebf21f66d22e54a22347efd375981188743afd99baacc342d88a99321235798725fedcbf43252669dade32415fee89da543bf23d4ex";

        private byte[] mySignature;
        private byte myVersion;
        private string myHexValue;

        public byte[] Signature
        {
            get { return mySignature; }
            set
            {
                if (!value.SequenceEqual(allowedSignature))
                {
                    throw new InvalidDataException();
                }
                else
                {
                    mySignature = value;
                }
            }
        }

        public byte Version
        {
            get { return myVersion; }
            set
            {
                if (!allowedVersions.Contains(value))
                {
                    throw new InvalidDataException();
                }
                else
                {
                    myVersion = value;
                }
            }
        }

        public string HexValue
        {
            get { return myHexValue; }
            set
            {
                if (!allowedHexValue.Equals(value))
                {
                    throw new InvalidDataException();
                }
                else
                {
                    myHexValue = value;
                }
            }
        }

        public byte[] XorKey { get; set; }
    }

    public class SuperBundle
    {
        private byte myInitialByte;

        public byte InitialByte
        {
            get { return myInitialByte; }
            set
            {
                if (value != 0x82)
                {
                    throw new InvalidDataException();
                }
                else
                {
                    myInitialByte = value;
                }
            }
        }

        public int Limit { get; set; }
        public SuperBundle Indirection { get; set; }
        public IEnumerable<BundleList> BundleCollections { get; set; }
        public Dictionary<string, PropertyValue> Properties { get; set; }
        public string Changed { get; set; }
    }

    public class BundleList
    {
        public IEnumerable<SuperBundle> Bundles { get; set; }
        public int Limit { get; set; }
        public string Name { get; set; }
        public byte Opcode
        {
            get { return 0x01; }
        }
    }

    public class PropertyValue
    {
        public PropertyValue(byte opcode, object value)
        {
            Opcode = opcode;
            Value = value;
        }

        public byte Opcode { get; private set; }
        public object Value { get; set; }
    }
}
