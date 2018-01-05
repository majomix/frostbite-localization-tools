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
            if (LoadedFilePath != null)
            {
                LoadFrostbiteStructure();
                Model.ExtractFiles(LoadedFilePath);
            }
        }

        public void Import()
        {
            if (LoadedFilePath != null)
            {
                LoadFrostbiteStructure();
                Model.ImportFiles(LoadedFilePath);
            }
        }

        public void ParseCommandLine()
        {
            OptionSet options = new OptionSet()
                .Add("export", value => Export = true)
                .Add("import", value => Export = false)
                .Add("toc=", value => LoadedFilePath = CreateFullPath(value, true))
                .Add("exedir=", value => ExeDirectoryPath = CreateFullPath(value, false))
                .Add("compression=", value => SetCompressionType(value));

            options.Parse(Environment.GetCommandLineArgs());
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

        private string CreateFullPath(string path, bool checkForFileExistence)
        {
            if (String.IsNullOrEmpty(path)) return null;

            if (path.Contains(':') && CheckForExistence(path, checkForFileExistence))
            {
                return path;
            }
            else
            {
                string resultPath = Directory.GetCurrentDirectory() + @"\" + path.Replace('/', '\\');
                if (CheckForExistence(resultPath, checkForFileExistence)) return resultPath;
                else return null;
            }
        }

        private bool CheckForExistence(string path, bool checkForFile)
        {
            if (checkForFile)
            {
                return File.Exists(path);
            }
            return true;
        }
    }
}
