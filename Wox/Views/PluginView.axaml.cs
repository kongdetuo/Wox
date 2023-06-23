using Avalonia.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using Wox.Infrastructure.UserSettings;
using Wox.ViewModel;

namespace Wox
{
    /// <summary>
    /// PluginView.xaml 的交互逻辑
    /// </summary>
    public partial class PluginView : Avalonia.Controls.UserControl
    {
        public PluginView()
        {
            InitializeComponent();
        }

        private Settings _settings = Settings.Instance;
        private PluginViewModel _viewModel => this.DataContext as PluginViewModel;
        private void OnPluginActionKeywordsClick(object? sender, Avalonia.Input.TappedEventArgs e)
        {

            var id = _viewModel.Metadata.ID;
            ActionKeywords changeKeywordsWindow = new ActionKeywords(id, _settings);
            changeKeywordsWindow.ShowDialog((Window)TopLevel.GetTopLevel(this)!);

        }

        private void OnPluginNameClick(object? sender, Avalonia.Input.TappedEventArgs e)
        {

            var website = _viewModel.Metadata.Website;
            if (!string.IsNullOrEmpty(website))
            {
                var uri = new Uri(website);
                if (Uri.CheckSchemeName(uri.Scheme))
                {
                    Process.Start(new ProcessStartInfo(website)
                    {
                        UseShellExecute = true
                    });
                }
            }

        }

        private void OnPluginDirecotyClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                var directory = _viewModel.Metadata.PluginDirectory;
                if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                {
                    Process.Start(new ProcessStartInfo(directory)
                    {
                        UseShellExecute = true
                    });
                }
            }
        }
    }
}
