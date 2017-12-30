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
            string swbf2TextPatchPath = @"H:\Origin\STAR WARS Battlefront II\Patch\Win32\loc\en.toc";
            string swbf2TextPath = @"H:\Origin\STAR WARS Battlefront II\Data\Win32\loc\en.toc";
            string swbf2uiPath = @"H:\Origin\STAR WARS Battlefront II\Data\Win32\ui.toc";

            using (FileStream fileStream = File.Open(swbf2TextPatchPath, FileMode.Open))
            {
                BundleBinaryReader reader = new BundleBinaryReader(fileStream);
                LoadStructureFromToc(reader, fileStream.Name);
                //ExtractFiles(fileStream.Name);
                ImportFiles(fileStream.Name);
            }
        }

        private void ResolveNewFiles(string tableOfContentsName, string directory)
        {
            FileInfo[] files = new DirectoryInfo(directory).GetFiles("*", SearchOption.AllDirectories);
            foreach (FileInfo file in files)
            {
                string[] pathLayers = Regex.Split(file.FullName.Substring(directory.Length), "(?:(bundles)|(chunks)|(ebx)|(res)|(chunkMeta)|(dbx))\\\\").Select(substring => substring.Trim('\\').Replace('\\', '/')).Where(_ => !String.IsNullOrEmpty(_)).ToArray();

                BundleList bundleList = myTableOfContents.Payload.BundleCollections.SingleOrDefault(_ => _.Name == pathLayers[0]);
                if (bundleList != null)
                {
                    SuperBundle correspondingBundle = null;
                    // no indirection
                    if(pathLayers.Length == 2)
                    {
                        correspondingBundle = bundleList.Bundles.SingleOrDefault(bundle => ((byte[])bundle.Properties["id"].Value).SequenceEqual(StringToByteArrayFastest(pathLayers[1])));
                    }
                    // indirection
                    else
                    {
                        IEnumerable<SuperBundle> superBundles = bundleList.Bundles
                            .Where(bundle => (string)bundle.Properties["id"].Value == pathLayers[1].Replace("_Dq_", ":"));

                        if(superBundles != null)
                        {
                            BundleList indirectBundleList = superBundles.SelectMany(bundle => bundle.Indirection.BundleCollections
                                    .Where(list => list.Name == pathLayers[2]))
                                    .SingleOrDefault();

                            if (indirectBundleList != null)
                            {
                                correspondingBundle = indirectBundleList.Bundles
                                    .SingleOrDefault(bundle => bundle.Properties.ContainsKey("name")
                                        ? (string)bundle.Properties["name"].Value == pathLayers[3]
                                        : ((byte[])bundle.Properties["id"].Value).SequenceEqual(StringToByteArrayFastest(pathLayers[3])));
                            }
                        }

                    }
                    if (correspondingBundle != null)
                    {
                        correspondingBundle.Changed = file.FullName;
                    }
                }
            }
        }

        private void ImportFiles(string tableOfContentsName)
        {
            string directory = Path.GetDirectoryName(tableOfContentsName) + @"\" + Path.GetFileNameWithoutExtension(tableOfContentsName);
            Random generator = new Random();

            ResolveNewFiles(tableOfContentsName, directory);

            bool isIndirect = myTableOfContents.Payload.BundleCollections.Where(bundleList => bundleList.Bundles.Count() > 0).First().Bundles.First().Indirection != null;
            
            List<bool> isIndirectionConsistentAndMatching = myTableOfContents.Payload.BundleCollections.Select(bundleList => bundleList.Bundles.All(bundle => (bundle.Indirection != null) == isIndirect)).Distinct().ToList();
            if (isIndirectionConsistentAndMatching.Count() != 1 || !isIndirectionConsistentAndMatching.First())
            {
                throw new ArgumentException("Mixed indirection bundles are not supported.");
            }

            string originalSuperBundlePath = Path.ChangeExtension(tableOfContentsName, "sb");
            string newSuperBundlePath = Path.ChangeExtension(tableOfContentsName, "sb_" + generator.Next().ToString());

            // overwrite files in superbundles
            if (!isIndirect)
            {
                // table of contents pointing directly to raw files in superbundles
                if(!myTableOfContents.Payload.Properties.ContainsKey("cas"))
                {
                    var allBundles = myTableOfContents.Payload.BundleCollections.SelectMany(bundleList => bundleList.Bundles).OrderBy(superBundle => (long)superBundle.Properties["offset"].Value);

                    using (BinaryReader originalReader = new BinaryReader(File.Open(originalSuperBundlePath, FileMode.Open)))
                    {
                        using (BundleBinaryWriter writer = new BundleBinaryWriter(File.Open(newSuperBundlePath, FileMode.Create)))
                        {
                            foreach (SuperBundle superBundle in allBundles)
                            {
                                Int64 initialFilePosition = writer.BaseStream.Position;

                                if (superBundle.Changed == null)
                                {
                                    long offset = (long)superBundle.Properties["offset"].Value;
                                    originalReader.BaseStream.Seek(offset, SeekOrigin.Begin);
                                    writer.Write(originalReader.ReadBytes((int)superBundle.Properties["size"].Value));
                                }
                                else
                                {
                                    superBundle.Properties["size"].Value = ChunkHandler.Chunk(writer, superBundle);
                                }
                                superBundle.Properties["offset"].Value = initialFilePosition;
                            }
                        }
                    }
                }
                // table of contents pointing directly to raw files in catalogues
                else
                {
                    var modifiedBundles = myTableOfContents.Payload.BundleCollections.SelectMany(bundleList => bundleList.Bundles).Where(bundle => bundle.Changed != null);

                    foreach (SuperBundle superBundle in modifiedBundles)
                    {
                        CatalogueEntry catEntry = FindCorrespondingCatalogueEntry((byte[])superBundle.Properties["sha1"].Value);
                        catEntry.Changed = true;

                        string pathToNewCascade = Path.GetDirectoryName(catEntry.Parent.Path) + @"\\" + "cas_99.cas";
                        catEntry.Archive = 99;

                        using (BundleBinaryWriter writer = new BundleBinaryWriter(File.Open(pathToNewCascade, FileMode.Append)))
                        {
                            catEntry.Offset = (int)writer.BaseStream.Position;
                            int fileSize = ChunkHandler.Chunk(writer, superBundle);
                            catEntry.Size = fileSize;

                            if(superBundle.Properties.ContainsKey("size"))
                            {
                                superBundle.Properties["size"].Value = fileSize;
                            }
                        }
                    }

                    foreach(Catalogue catalogue in myCatalogues.Where(_ => _.Files.Values.Any(entry => entry.Changed)))
                    {
                        using (BundleBinaryWriter writer = new BundleBinaryWriter(File.Open(catalogue.Path + "_tmp", FileMode.Create)))
                        {
                            writer.Write(catalogue);
                        }
                    }
                }

            }
            // write catalogues
            else
            {
                
            }

            // write table of contents OK
            string randomName = Path.ChangeExtension(tableOfContentsName, @".toc_" + generator.Next().ToString());

            using (BundleBinaryWriter writer = new BundleBinaryWriter(File.Open(randomName, FileMode.Create)))
            {
                writer.Write(myTableOfContents.Header);
                writer.Write(myTableOfContents.Payload);
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

                string directory = Path.GetDirectoryName(tableOfContentsName) + string.Concat(Enumerable.Repeat(@"\..", 2));
                FileInfo[] catalogueFiles = new DirectoryInfo(directory).GetFiles("*.cat", SearchOption.AllDirectories);

                List<Catalogue> currentCatalogues = new List<Catalogue>();
                foreach (var file in catalogueFiles)
                {
                    using (BundleBinaryReader catReader = new BundleBinaryReader(File.Open(file.FullName, FileMode.Open)))
                    {
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

            // extract raw files from superbundles
            if (!myTableOfContents.Payload.Properties.ContainsKey("cas"))
            {
                foreach (var collection in myTableOfContents.Payload.BundleCollections)
                {
                    string collectionDirectory = directory + @"\" + collection.Name + @"\";
                    foreach (var item in collection.Bundles)
                    {
                        if (item.Properties.ContainsKey("offset") && item.Properties.ContainsKey("size"))
                        {
                            Directory.CreateDirectory(collectionDirectory);

                            using (FileStream inputFileStream = File.Open(superBundlePath, FileMode.Open))
                            {
                                BinaryReader reader = new BinaryReader(inputFileStream);

                                long a = (long)item.Properties["offset"].Value;
                                reader.BaseStream.Seek((int)a, SeekOrigin.Begin);

                                string finalName = collectionDirectory + ByteArrayToString((byte[])item.Properties["id"].Value);

                                ChunkHandler.Dechunk(finalName, reader, (int)item.Properties["size"].Value);
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

                        string finalName = directory + @"\" + entry.ResolvedName.Replace(":", "_Dq_");
                        Directory.CreateDirectory(finalName.Substring(0, finalName.LastIndexOf('\\')));

                        ChunkHandler.Dechunk(finalName, reader, entry.Size);
                    }
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