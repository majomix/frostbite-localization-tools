using FrostbiteFileSystemTools.Model;
using System;
using System.ComponentModel;
using System.IO;

namespace FrostbiteFileSystemTools.ViewModel
{
    internal abstract class BaseViewModel : INotifyPropertyChanged
    {
        private int myCurrentProgress = 100;
        private string myLoadedFilePath;
        private string myExeDirectoryPath;
        private bool myHasError;

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
            get { return myExeDirectoryPath ?? Directory.GetCurrentDirectory(); }
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
        public bool HasError
        {
            get { return myHasError; }
            set
            {
                if (myHasError != value)
                {
                    myHasError = value;
                    OnPropertyChanged("HasError");
                }
            }
        }
        public string CompleteFilePath
        {
            get { return ExeDirectoryPath + @"\" + LoadedFilePath; }
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
            if(File.Exists(CompleteFilePath))
            {
                Model.LoadStructureFromToc(CompleteFilePath, ExeDirectoryPath);
                OnPropertyChanged("Model");
            }
            else
            {
                HasError = true;
            }
        }
    }
}
