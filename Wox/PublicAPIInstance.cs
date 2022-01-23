using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using Wox.Core.Plugin;
using Wox.Core.Resource;
using Wox.Helper;
using Wox.Infrastructure.Hotkey;
using Wox.Plugin;
using Wox.ViewModel;

namespace Wox
{
    public class PublicAPIInstance : IPublicAPI
    {
        private readonly SettingWindowViewModel _settingsVM;
        private readonly MainViewModel _mainVM;

        #region Constructor

        public PublicAPIInstance(SettingWindowViewModel settingsVM, MainViewModel mainVM)
        {
            _settingsVM = settingsVM;
            _mainVM = mainVM;
            GlobalHotkey.Instance.hookedKeyboardCallback += KListener_hookedKeyboardCallback;
            WebRequest.RegisterPrefix("data", new DataWebRequestFactory());
        }

        #endregion Constructor

        #region Public API

        public void ChangeQuery(string query, bool requery = false)
        {
            if (requery)
            {
                _mainVM.SelectedResults = _mainVM.Results;
                if (_mainVM.QueryText == query)
                    _mainVM.ChangeQueryText(string.Empty); // ensure queryText will be change or equal to string.Empty
            }
            _mainVM.ChangeQueryText(query);
        }

        public void RestarApp()
        {
            _mainVM.MainWindowVisibility = Visibility.Hidden;

            // we must manually save
            // UpdateManager.RestartApp() will call Environment.Exit(0)
            // which will cause ungraceful exit
            SaveAppAllSettings();

       
        }

        public void CheckForNewUpdate()
        {
           
        }

        public void SaveAppAllSettings()
        {
            _mainVM.Save();
            _settingsVM.Save();
            PluginManager.Save();
        }

        public void ReloadAllPluginData()
        {
            PluginManager.ReloadData();
        }

        public void ShowMsg(string title, string subTitle = "", string iconPath = "")
        {
            ShowMsg(title, subTitle, iconPath, true);
        }

        public void ShowMsg(string title, string subTitle, string iconPath, bool useMainWindowAsOwner = true)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var msg = useMainWindowAsOwner ? new Msg { Owner = Application.Current.MainWindow } : new Msg();
                msg.Show(title, subTitle, iconPath);
            });
        }

        public void OpenSettingDialog()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                SettingWindow sw = SingletonWindowOpener.Open<SettingWindow>(this, _settingsVM);
            });
        }

        public void InstallPlugin(string path)
        {
            Application.Current.Dispatcher.Invoke(() => PluginManager.InstallPlugin(path));
        }

        public string GetTranslation(string key)
        {
            return InternationalizationManager.Instance.GetTranslation(key);
        }

        public List<PluginProxy> GetAllPlugins()
        {
            return PluginManager.AllPlugins.ToList();
        }

        public event WoxGlobalKeyboardEventHandler GlobalKeyboardEvent;

        #endregion Public API

        #region Private Methods

        private bool KListener_hookedKeyboardCallback(KeyEvent keyevent, int vkcode, SpecialKeyState state)
        {
            if (GlobalKeyboardEvent != null)
            {
                return GlobalKeyboardEvent((int)keyevent, vkcode, state);
            }
            return true;
        }

        internal void HideWindow()
        {
            _mainVM.MainWindowVisibility = Visibility.Collapsed;
        }

        #endregion Private Methods
    }
}