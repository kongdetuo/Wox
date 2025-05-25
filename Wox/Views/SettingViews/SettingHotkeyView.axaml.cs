using Avalonia.Controls;
using NHotkey.Wpf;
using NHotkey;
using System;
using Wox.Core.Resource;
using Wox.Infrastructure.Hotkey;
using Avalonia.Interactivity;
using Wox.ViewModel;
using Wox.Infrastructure.UserSettings;

namespace Wox.Views.SettingViews
{
    public partial class SettingHotkeyView : UserControl
    {
        private SettingWindowViewModel _viewModel => (SettingWindowViewModel)this.DataContext!;
        private Settings _settings = Settings.Instance;
        public SettingHotkeyView()
        {
            InitializeComponent();
        }
        #region Hotkey

        private void OnHotkeyControlLoaded(object sender, RoutedEventArgs e)
        {
            //HotkeyControl.SetHotkey(_viewModel.Settings.Hotkey, false);
        }

        void OnHotkeyChanged(object sender, EventArgs e)
        {
            //if (HotkeyControl.CurrentHotkeyAvailable)
            //{
            //    SetHotkey(HotkeyControl.CurrentHotkey, (o, args) =>
            //    {
            //        if (!Application.Current.MainWindow.IsVisible)
            //        {
            //            Application.Current.MainWindow.Visibility = Visibility.Visible;
            //        }
            //        else
            //        {
            //            Application.Current.MainWindow.Visibility = Visibility.Hidden;
            //        }
            //    });
            //    RemoveHotkey(_settings.Hotkey);
            //    _settings.Hotkey = HotkeyControl.CurrentHotkey.ToString();
            //}
        }

        void SetHotkey(HotkeyModel hotkey, EventHandler<HotkeyEventArgs> action)
        {
            string hotkeyStr = hotkey.ToString();
            try
            {
                HotkeyManager.Current.AddOrReplace(hotkeyStr, hotkey.CharKey, hotkey.ModifierKeys, action);
            }
            catch (Exception)
            {
                string errorMsg =
                    string.Format(InternationalizationManager.Instance.GetTranslation("registerHotkeyFailed"), hotkeyStr);
                //   MessageBox.Show(errorMsg);
            }
        }

        void RemoveHotkey(string hotkeyStr)
        {
            if (!string.IsNullOrEmpty(hotkeyStr))
            {
                HotkeyManager.Current.Remove(hotkeyStr);
            }
        }

        private void OnDeleteCustomHotkeyClick(object sender, RoutedEventArgs e)
        {
            var item = _viewModel.SelectedCustomPluginHotkey;
            if (item == null)
            {
                //    MessageBox.Show(InternationalizationManager.Instance.GetTranslation("pleaseSelectAnItem"));
                return;
            }

            string deleteWarning =
                string.Format(InternationalizationManager.Instance.GetTranslation("deleteCustomHotkeyWarning"),
                    item.Hotkey);
            //if (
            //    MessageBox.Show(deleteWarning, InternationalizationManager.Instance.GetTranslation("delete"),
            //        MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            //{
            //    _settings.CustomPluginHotkeys.Remove(item);
            //    RemoveHotkey(item.Hotkey);
            //}
        }

        private void OnnEditCustomHotkeyClick(object sender, RoutedEventArgs e)
        {
            var item = _viewModel.SelectedCustomPluginHotkey;
            if (item != null)
            {
                //Window window = (Window)TopLevel.GetTopLevel(this);
                //CustomQueryHotkeySetting window = new(this, _settings);
                //window.UpdateItem(item);
                //window.ShowDialog(this);
            }
            else
            {
                // MessageBox.Show(InternationalizationManager.Instance.GetTranslation("pleaseSelectAnItem"));
            }
        }

        private void OnAddCustomeHotkeyClick(object sender, RoutedEventArgs e)
        {
            //new CustomQueryHotkeySetting(this, _settings).ShowDialog(this);
        }

        #endregion


    }
}
