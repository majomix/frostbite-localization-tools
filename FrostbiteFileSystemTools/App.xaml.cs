using FrostbiteFileSystemTools.Model;
using FrostbiteFileSystemTools.View;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace FrostbiteFileSystemTools
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            FrostbiteEditor f = new FrostbiteEditor();
            MainEditorWindow mainEditorWindow = new MainEditorWindow();
            MainWindow = mainEditorWindow;
            MainWindow.Close();
            //try
            //{
            //    FrostbiteEditor f = new FrostbiteEditor();
            //}
            //catch(InvalidDataException ex)
            //{
            //    Console.WriteLine(ex.Message);
            //}
            if (Environment.GetCommandLineArgs().Length > 1)
            {
                //OneTimeRunWindow oneTimeRunWindow = new OneTimeRunWindow();
                //MainWindow = oneTimeRunWindow;
                //MainWindow.Show();
            }
            else
            {
                //MainEditorWindow mainEditorWindow = new MainEditorWindow();
                //MainWindow = mainEditorWindow;
                //MainWindow.Show();
            }
        }
    }
}
