using System;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Navigation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Wox.Core.Plugin;
using Wox.Infrastructure.UserSettings;
using Wox.Plugin;
using Wox.ViewModel;

namespace Wox
{
    public partial class SettingWindow : Window
    {

        public readonly IPublicAPI _api;
        private Settings _settings;
        private SettingWindowViewModel _viewModel;

        public SettingWindow(IPublicAPI api, SettingWindowViewModel viewModel)
        {
            InitializeComponent();
            _settings = Settings.Instance;
            DataContext = viewModel;
            _viewModel = viewModel;
            _api = api;

            this.Closed += OnClosed;
        }




        private void OnCheckUpdates(object sender, RoutedEventArgs e)
        {
            //_viewModel.UpdateApp(); // TODO: change to command
        }

        private void OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void OnClosed(object sender, EventArgs e)
        {
            _viewModel.Save();
        }

        private void OnCloseExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }
    }
}
