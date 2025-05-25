using Avalonia.Input.Platform;
using Avalonia.Threading;
using Splat;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Wox.Core.Plugin;
using Wox.Core.Resource;
using Wox.Helper;
using Wox.Infrastructure.Hotkey;
using Wox.Plugin;
using Wox.Plugin.Services;
using Wox.ViewModel;

namespace Wox
{
    public class PublicAPIInstance : IPublicAPI
    {
        public SettingWindowViewModel SettingsVM { get; set; }
        public MainViewModel MainVM { get; set; }

        public IClipboardService Clipboard { get; set; }

        public IIconHelper IconHelper { get; } = new IconHelper();

        #region Constructor

        public PublicAPIInstance()
        {

            GlobalHotkey.Instance.hookedKeyboardCallback += KListener_hookedKeyboardCallback;
            WebRequest.RegisterPrefix("data", new DataWebRequestFactory());
        }

        #endregion Constructor

        #region Public API

        public void ChangeQuery(string query, bool requery = false)
        {
            if (requery)
            {
                MainVM.SelectedResults = MainVM.Results;
                if (MainVM.QueryText == query)
                    MainVM.ChangeQueryText(string.Empty); // ensure queryText will be change or equal to string.Empty
            }
            MainVM.ChangeQueryText(query);
        }

        public void RestarApp()
        {
            MainVM.ShowMainWindow = false;

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
            MainVM.Save();
            SettingsVM.Save();
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
            Dispatcher.UIThread.Invoke(() =>
            {
                var msg = useMainWindowAsOwner ? new Msg() : new Msg();
                msg.Show(title, subTitle, iconPath);
            });

        }

        public void OpenSettingDialog()
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                //SingletonWindowOpener.Open<SettingWindow>(this, SettingsVM);

                SettingWindow sw = new SettingWindow(this, SettingsVM);
                sw.Show();
            });
        }

        public void InstallPlugin(string path)
        {
            //Application.Current.Dispatcher.Invoke(() => PluginManager.InstallPlugin(path));
        }

        public string GetTranslation(string key)
        {
            return InternationalizationManager.Instance.GetTranslation(key);
        }

        public List<PluginMetadata> GetAllPlugins()
        {
            return PluginManager.AllPlugins.Select(p=>p.Metadata).ToList();
        }

       

        public void ShowWox()
        {
            MainVM.ShowMainWindow = false;
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
            MainVM.ShowMainWindow = false;
        }
        #endregion Private Methods
    }


    class ClipboardService : IClipboardService
    {
        Avalonia.Input.Platform.IClipboard Clipboard { get; set; }

        public ClipboardService(IClipboard clipboard)
        {
            Clipboard = clipboard;
        }

        public async Task SetTextAsync(string? text)
        {
            await Clipboard.SetTextAsync(text);
        }
    }
}