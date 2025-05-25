using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Win32;
using System;
using System.IO;
using Wox.Core.Plugin;
using Wox.Infrastructure.UserSettings;

namespace Wox.Views.SettingViews
{
    public partial class SettingGeneralView : UserControl
    {
        private const string StartupPath = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private Settings  _settings = Settings.Instance;
        public SettingGeneralView()
        {
            InitializeComponent();
        }
        #region General

        private void OnAutoStartupChecked(object sender, RoutedEventArgs e)
        {
            SetStartup();
        }

        private void OnAutoStartupUncheck(object sender, RoutedEventArgs e)
        {
            RemoveStartup();
        }

        public static void SetStartup()
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupPath, true);
            key?.SetValue(Infrastructure.Constant.Wox, Infrastructure.Constant.ExecutablePath);
        }

        private void RemoveStartup()
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupPath, true);
            key?.DeleteValue(Infrastructure.Constant.Wox, false);
        }

        public static bool StartupSet()
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupPath, true);
            if (key?.GetValue(Infrastructure.Constant.Wox) is string path)
            {
                return path == Infrastructure.Constant.ExecutablePath;
            }
            else
            {
                return false;
            }
        }

        private void OnSelectPythonDirectoryClick(object sender, RoutedEventArgs e)
        {
            //var dlg = new VistaFolderBrowserDialog()
            //{
            //    SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
            //};

            //var result = dlg.ShowDialog();
            //if (result == true)
            //{
            //    string pythonDirectory = dlg.SelectedPath;
            //    if (!string.IsNullOrEmpty(pythonDirectory))
            //    {
            //        var pythonPath = Path.Combine(pythonDirectory, PluginsLoader.PythonExecutable);
            //        if (File.Exists(pythonPath))
            //        {
            //            _settings.PluginSettings.PythonDirectory = pythonDirectory;
            //            //   MessageBox.Show("Remember to restart Wox use new Python path");
            //        }
            //        else
            //        {
            //            //  MessageBox.Show("Can't find python in given directory");
            //        }
            //    }
            //}
        }

        #endregion

    }
}
