using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FrostbiteFileSystemTools.Model
{
    internal class FrostbiteEditor
    {
        private TableOfContents myTableOfContents;
        private IEnumerable<Catalogue> myCatalogues;

        public FrostbiteEditor()
        {
            string rivalsTextPath = @"E:\Program Files\Origin Games\Need for Speed(TM) Rivals\Update\Patch\Data\Win32\Loc\en.toc";
            string rivalsFontsPath = @"E:\Program Files\Origin Games\Need for Speed(TM) Rivals\Update\Patch\Data\Win32\UI.toc";
            string starwarsTextPath = @"H:\Origin\STAR WARS Battlefront\Patch\Win32\loc\en.toc";
            string starwarsFontsPath = @"H:\Origin\STAR WARS Battlefront\Patch\Win32\ui.toc";
            string fifaTextsPath = @"H:\Origin\FIFA 17 DEMO\Data\Win32\loc\en.toc";
            string nfsTextPath = @"H:\Origin\Need for Speed\Update\Patch\Data\Win32\loc\en.toc";
            string nfsFontsPath = @"H:\Origin\Need for Speed\Data\Win32\ui.toc";

            using (FileStream fileStream = File.Open(rivalsTextPath, FileMode.Open))
            {
                BundleBinaryReader reader = new BundleBinaryReader(fileStream);
                LoadStructureFromToc(reader, fileStream.Name);
                //ExtractFiles(fileStream.Name);
                ImportFiles(fileStream.Name);
            }


        }

        private void ImportFiles(string tableOfContentsName)
        {
            string directory = Path.GetDirectoryName(tableOfContentsName) + @"\" + Path.GetFileNameWithoutExtension(tableOfContentsName);
            string[] files = Directory.GetFiles(directory);
            foreach(string file in files)
            {
                string pattern = "(<)|(>)|(!)|(,)|(\\(#PCDATA\\))|(\\()|(\\))|(\")|(\\?)|(\\*)|(\\|)| ";

                // split line via delimeter
                //string[] substrings = Regex.Split(s, pattern);
                //file.Split()
            }
        }

        public void LoadStructureFromToc(BundleBinaryReader bundleReader, string tableOfContentsName)
        {
            myTableOfContents = new TableOfContents();
            myTableOfContents.Header = bundleReader.ReadTableOfContentsHeader();
            myTableOfContents.Payload = bundleReader.ReadTableOfContentsPayload();
            string superBundlePath = Path.ChangeExtension(tableOfContentsName, "sb");

            if (myTableOfContents.Payload.Properties.ContainsKey("cas"))
            {
                using (FileStream inputFileStream = File.Open(superBundlePath, FileMode.Open))
                {
                    BundleBinaryReader sbReader = new BundleBinaryReader(inputFileStream);

                    foreach (var collection in myTableOfContents.Payload.BundleCollections)
                    {
                        foreach (var item in collection.Bundles)
                        {
                            if (!item.Properties.ContainsKey("sha1") && !item.Properties.ContainsKey("base"))
                            {
                                long offset = (long)item.Properties["offset"].Value;
                                sbReader.BaseStream.Seek((int)offset, SeekOrigin.Begin);
                                item.Indirection = sbReader.ReadTableOfContentsPayload();
                            }
                        }
                    }
                }

                string directory = Path.GetDirectoryName(tableOfContentsName) + string.Concat(Enumerable.Repeat(@"\..", 4));
                FileInfo[] catalogueFiles = new DirectoryInfo(directory).GetFiles("*.cat", SearchOption.AllDirectories);

                List<Catalogue> currentCatalogues = new List<Catalogue>();
                foreach (var file in catalogueFiles)
                {
                    using (FileStream inputFileStream = File.Open(file.FullName, FileMode.Open))
                    {
                        BundleBinaryReader catReader = new BundleBinaryReader(inputFileStream);
                        currentCatalogues.Add(catReader.ReadCatalogue(myTableOfContents.Header.Version, file.FullName));
                    }
                }
                myCatalogues = currentCatalogues;
            }
        }

        private void ExtractFiles(string tableOfContentsName)
        {
            string superBundlePath = Path.ChangeExtension(tableOfContentsName, "sb");
            string directory = Path.GetDirectoryName(tableOfContentsName) + @"\" + Path.GetFileNameWithoutExtension(tableOfContentsName);
            StringBuilder decompressBatBuilder = new StringBuilder();
            StringBuilder compressBatBuilder = new StringBuilder();

            // extract raw files from superbundles
            if (!myTableOfContents.Payload.Properties.ContainsKey("cas"))
            {
                foreach (var collection in myTableOfContents.Payload.BundleCollections)
                {
                    foreach (var item in collection.Bundles)
                    {
                        if (item.Properties.ContainsKey("offset") && item.Properties.ContainsKey("size"))
                        {
                            Directory.CreateDirectory(directory);

                            using (FileStream inputFileStream = File.Open(superBundlePath, FileMode.Open))
                            {
                                BinaryReader reader = new BinaryReader(inputFileStream);

                                long a = (long)item.Properties["offset"].Value;
                                reader.BaseStream.Seek((int)a, SeekOrigin.Begin);

                                string finalName = directory + @"\" + ByteArrayToString((byte[])item.Properties["id"].Value);
                                decompressBatBuilder.AppendLine(BuildDecompressionLine(finalName));
                                compressBatBuilder.AppendLine(BuildCompressionLine(finalName));
                                using (FileStream outputFileStream = File.Open(finalName, FileMode.Create))
                                {
                                    using (BinaryWriter writer = new BinaryWriter(outputFileStream))
                                    {
                                        writer.Write(reader.ReadBytes((int)item.Properties["size"].Value));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            // extract from a catalogue
            else
            {
                foreach(CatalogueEntry entry in BuildFileListInCatalogues(myTableOfContents.Payload.BundleCollections))
                {
                    string compoundName = Path.GetDirectoryName(entry.Parent.Path) + @"\cas_" + entry.Archive.ToString("00") + ".cas";
                    using (FileStream inputFileStream = File.Open(compoundName, FileMode.Open))
                    {
                        BinaryReader reader = new BinaryReader(inputFileStream);
                        reader.BaseStream.Seek(entry.Offset, SeekOrigin.Begin);

                        string finalName = directory + @"\" + entry.ResolvedName.Replace(":", "");
                        decompressBatBuilder.AppendLine(BuildDecompressionLine(finalName));
                        compressBatBuilder.AppendLine(BuildCompressionLine(finalName));
                        Directory.CreateDirectory(finalName.Substring(0, finalName.LastIndexOf('\\')));

                        using (FileStream outputFileStream = File.Open(finalName, FileMode.Create))
                        {
                            using (BinaryWriter writer = new BinaryWriter(outputFileStream))
                            {
                                writer.Write(reader.ReadBytes(entry.Size));
                            }
                        }
                    }
                }

            }

            using (FileStream batFileStream = File.Open(directory + @"\..\" + Path.GetFileNameWithoutExtension(tableOfContentsName) + @"_compress.bat", FileMode.Create))
            {
                using (StreamWriter writer = new StreamWriter(batFileStream))
                {
                    writer.Write(compressBatBuilder.ToString());
                }
            }

            using (FileStream batFileStream = File.Open(directory + @"\..\" + Path.GetFileNameWithoutExtension(tableOfContentsName) + @"_decompress.bat", FileMode.Create))
            {
                using (StreamWriter writer = new StreamWriter(batFileStream))
                {
                    writer.Write(decompressBatBuilder.ToString());
                }
            }
        }

        private CatalogueEntry FindCorrespondingCatalogueEntry(byte[] sha1)
        {
            foreach(var catalogue in myCatalogues)
            {
                if(catalogue.Files.ContainsKey(sha1))
                {
                    return catalogue.Files[sha1];
                }
            }

            return null;
        }

        private IEnumerable<CatalogueEntry> BuildFileListInCatalogues(IEnumerable<BundleList> bundleCollections)
        {
            foreach (var collection in bundleCollections)
            {
                string appendPrefix = collection.Name;

                foreach (var item in collection.Bundles)
                {
                    // using a level of indirection
                    if (item.Indirection != null)
                    {
                        if (item.Properties.ContainsKey("id"))
                        {
                            appendPrefix = collection.Name + @"\" + ((string)item.Properties["id"].Value).Replace(@"/", @"\");
                        }
                        foreach (var entry in BuildFileListInCatalogues(item.Indirection.BundleCollections))
                        {
                            entry.ResolvedName = appendPrefix + @"\" + entry.ResolvedName;
                            yield return entry;
                        }
                    }
                    // directly
                    else
                    {
                        if(item.Properties.ContainsKey("sha1"))
                        {
                            byte[] sha1 = (byte[])item.Properties["sha1"].Value;

                            CatalogueEntry catalogueEntry = FindCorrespondingCatalogueEntry(sha1);
                            if (catalogueEntry != null)
                            {
                                if (item.Properties.ContainsKey("name"))
                                {
                                    catalogueEntry.ResolvedName = ((string)item.Properties["name"].Value).Replace(@"/", @"\");
                                }
                                else if (item.Properties.ContainsKey("id"))
                                {
                                    catalogueEntry.ResolvedName = ByteArrayToString((byte[])item.Properties["id"].Value);
                                }
                                catalogueEntry.ResolvedName = appendPrefix + @"\" + catalogueEntry.ResolvedName;

                                yield return catalogueEntry;
                            }
                        }
                    }
                }
            }
        }

        private string ByteArrayToString(byte[] byteArray)
        {
            return BitConverter.ToString(byteArray).Replace("-", string.Empty).ToLower();
        }

        private string BuildDecompressionLine(string file)
        {
            return "nfsrlz -d \"" + file + "\" \"" + file + ".dec\"";
        }

        private string BuildCompressionLine(string file)
        {
            return "nfsrlz -c \"" + file + ".dec\" \"" + file + "\"";
            
        }

        private byte[] StringToByteArrayFastest(string hex)
        {
            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }

            return arr;
        }

        private int GetHexVal(char hex)
        {
            int val = (int)hex;
            return val - (val < 58 ? 48 : 87);
        }
    }
}