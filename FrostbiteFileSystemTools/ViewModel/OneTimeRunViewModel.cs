using FrostbiteFileSystemTools.Model;
using FrostbiteFileSystemTools.ViewModel.Commands;
using NDesk.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace FrostbiteFileSystemTools.ViewModel
{
    internal class OneTimeRunViewModel : BaseViewModel
    {
        public bool? Export { get; set; }
        public ICommand ExtractByParameterCommand { get; private set; }
        public ICommand ImportByParameterCommand { get; private set; }

        public OneTimeRunViewModel()
        {
            ParseCommandLine();
            Model = new FrostbiteEditor();

            ImportByParameterCommand = new ImportByParameterCommand();
            ExtractByParameterCommand = new ExtractByParameterCommand();
        }

        public void Extract()
        {
            LoadStructureAndDoWork(Model.ExtractFiles);
        }

        public void Import()
        {
            LoadStructureAndDoWork(Model.ImportFiles);
        }

        public void ParseCommandLine()
        {
            OptionSet options = new OptionSet()
                .Add("export", value => Export = true)
                .Add("import", value => Export = false)
                .Add("toc=", value => LoadedFilePath = TrimPath(value))
                .Add("exedir=", value => ExeDirectoryPath = CreateExeDirPath(value))
                .Add("compression=", value => SetCompressionType(value));

            options.Parse(Environment.GetCommandLineArgs());
        }

        private void LoadStructureAndDoWork(Action<string> function)
        {
            try
            {
                LoadFrostbiteStructure();
                function(CompleteFilePath);
            }
            catch(Exception e)
            {
                HasError = true;
            }
        }

        private void SetCompressionType(string value)
        {
            Dictionary<string, ICompressionStrategy> availableCompressions = new Dictionary<string, ICompressionStrategy>()
            {
                { "zstd", new CompressionStrategyZstd() },
                { "lz4", new CompressionStrategyLZ4() }
            };

            if(availableCompressions.ContainsKey(value))
            {
                ChunkHandler.CompressionStrategy = availableCompressions[value];
            }
        }

        private string CreateExeDirPath(string path)
        {
            string finalPath = null;

            if (!String.IsNullOrEmpty(path))
            {
                if (path.Contains(':') && Directory.Exists(path))
                {
                    finalPath = path;
                }
                else
                {
                    string resultPath = Directory.GetCurrentDirectory() + @"\" + path.Replace('/', '\\');
                    if (Directory.Exists(path))
                    {
                        finalPath = resultPath;
                    }
                }
            }

            return TrimPath(finalPath);
        }

        private string TrimPath(string path)
        {
            return path.Trim('\\');
        }
    }
}
