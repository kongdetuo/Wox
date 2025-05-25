using Avalonia.Controls;
using Avalonia.Interactivity;
using Wox.ViewModel;

namespace Wox.Views.SettingViews
{
    public partial class SettingProxyView : UserControl
    {
        public SettingProxyView()
        {
            //InitializeComponent();
        }

        private SettingWindowViewModel _viewModel => (SettingWindowViewModel)this.DataContext!;

        #region Proxy

        private void OnTestProxyClick(object sender, RoutedEventArgs e)
        { // TODO: change to command
            var msg = _viewModel.TestProxy();
            //MessageBox.Show(msg); // TODO: add message box service
        }

        #endregion

    }
}
