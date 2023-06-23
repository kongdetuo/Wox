

using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using NHotkey.Wpf;
using System;
using System.Threading.Tasks;
using Wox.Core.Resource;
using Wox.Infrastructure.Hotkey;
using Wox.Plugin;

namespace Wox
{
    public partial class HotkeyControl : Avalonia.Controls.UserControl
    {
        public HotkeyModel CurrentHotkey { get; private set; }
        public bool CurrentHotkeyAvailable { get; private set; }

        public event EventHandler HotkeyChanged;

        protected virtual void OnHotkeyChanged()
        {
            EventHandler handler = HotkeyChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        public HotkeyControl()
        {
            InitializeComponent();
            this.KeyDown += HotkeyControl_KeyDown; ;  
        }

        private void HotkeyControl_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            return;
            //e.Handled = true;
            //tbMsg.IsVisible = false;

            ////when alt is pressed, the real key should be e.SystemKey
            //Key key = (e.Key == Key.System ? e.Key : e.Key);

            //SpecialKeyState specialKeyState = GlobalHotkey.Instance.CheckModifiers();

            //var hotkeyModel = new HotkeyModel(
            //    specialKeyState.AltPressed,
            //    specialKeyState.ShiftPressed,
            //    specialKeyState.WinPressed,
            //    specialKeyState.CtrlPressed,
            //    key);

            //var hotkeyString = hotkeyModel.ToString();

            //if (hotkeyString == tbHotkey.Text)
            //{
            //    return;
            //}

            //Dispatcher.UIThread.InvokeAsync(async () =>
            //{
            //    await Task.Delay(500);
            //    SetHotkey(hotkeyModel);
            //});
        }

        public void SetHotkey(HotkeyModel keyModel, bool triggerValidate = true)
        {
            CurrentHotkey = keyModel;

            tbHotkey.Text = CurrentHotkey.ToString();
            tbHotkey.SelectAll();

            if (triggerValidate)
            {
                CurrentHotkeyAvailable = CheckHotkeyAvailability();
                if (!CurrentHotkeyAvailable)
                {
                    tbMsg.Foreground = new SolidColorBrush(Colors.Red);
                    tbMsg.Text = InternationalizationManager.Instance.GetTranslation("hotkeyUnavailable");
                }
                else
                {
                    tbMsg.Foreground = new SolidColorBrush(Colors.Green);
                    tbMsg.Text = InternationalizationManager.Instance.GetTranslation("success");
                }
                tbMsg.IsVisible = true;
                OnHotkeyChanged();
            }
        }

        public void SetHotkey(string keyStr, bool triggerValidate = true)
        {
            SetHotkey(new HotkeyModel(keyStr), triggerValidate);
        }

        private bool CheckHotkeyAvailability()
        {
            try
            {
                HotkeyManager.Current.AddOrReplace("HotkeyAvailabilityTest", CurrentHotkey.CharKey, CurrentHotkey.ModifierKeys, (sender, e) => { });

                return true;
            }
            catch
            {
            }
            finally
            {
                HotkeyManager.Current.Remove("HotkeyAvailabilityTest");
            }

            return false;
        }

        public new bool IsFocused
        {
            get { return tbHotkey.IsFocused; }
        }
    }
}
