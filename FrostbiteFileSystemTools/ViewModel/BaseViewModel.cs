using FrostbiteFileSystemTools.Model;
using System;
using System.ComponentModel;

namespace FrostbiteFileSystemTools.ViewModel
{
    internal abstract class BaseViewModel : INotifyPropertyChanged
    {
        private int myCurrentProgress;
        private string myLoadedFilePath;
        private string myExeDirectoryPath;

        public FrostbiteEditor Model { get; protected set; }
        public string LoadedFilePath
        {
            get { return myLoadedFilePath; }
            set
            {
                if (myLoadedFilePath != value)
                {
                    myLoadedFilePath = value;
                    OnPropertyChanged("LoadedFilePath");
                }
            }
        }
        public string ExeDirectoryPath
        {
            get { return myExeDirectoryPath; }
            set
            {
                if (myExeDirectoryPath != value)
                {
                    myExeDirectoryPath = value;
                    OnPropertyChanged("ExeDirectoryPath");
                }
            }
        }
        public int CurrentProgress
        {
            get { return myCurrentProgress; }
            protected set
            {
                if (myCurrentProgress != value)
                {
                    myCurrentProgress = value;
                    OnPropertyChanged("CurrentProgress");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler RequestClose;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler propertyChanged = PropertyChanged;
            if (propertyChanged != null)
            {
                propertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void OnRequestClose(EventArgs e)
        {
            RequestClose(this, e);
        }

        public void LoadFrostbiteStructure()
        {
            Model.LoadStructureFromToc(LoadedFilePath, CalculateNumberOfFolders());
            OnPropertyChanged("Model");
        }

        private int CalculateNumberOfFolders()
        {
            string pathDifference = LoadedFilePath.Split(new string[] { ExeDirectoryPath }, StringSplitOptions.RemoveEmptyEntries)[0];
            int fullTocSlashes = LoadedFilePath.Split('\\').Length - 2;
            int pathDifferenceSlahses = pathDifference.Split('\\').Length - 2;

            if (pathDifferenceSlahses >= fullTocSlashes)
            {
                throw new ArgumentException("Wrong path arguments.");
            }
            return pathDifferenceSlahses;
        }
    }
}
