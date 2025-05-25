using Avalonia.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using Wox.Infrastructure.UserSettings;
using Wox.ViewModel;
namespace Wox.Views.SettingViews
{
    public partial class SettingPluginView : UserControl
    {
        private Settings _settings = Settings.Instance;
        public SettingPluginView()
        {
            InitializeComponent();
        }

        private SettingWindowViewModel _viewModel => (SettingWindowViewModel)this.DataContext;

        private void OnPluginActionKeywordsClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                var id = _viewModel.SelectedPlugin?.PluginPair.Metadata.ID;
                if (id != null)
                {
                    ActionKeywords changeKeywordsWindow = new(id, _settings);
                    changeKeywordsWindow.ShowDialog((Window)TopLevel.GetTopLevel(this));
                }
            }
        }

        private void OnPluginNameClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                var website = _viewModel.SelectedPlugin?.PluginPair.Metadata.Website;
                if (!string.IsNullOrEmpty(website))
                {
                    var uri = new Uri(website);
                    if (Uri.CheckSchemeName(uri.Scheme))
                    {
                        Process.Start(website);
                    }
                }
            }
        }

        private void OnPluginDirecotyClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                var directory = _viewModel.SelectedPlugin?.PluginPair.Metadata.PluginDirectory;
                if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                {
                    Process.Start(directory);
                }
            }
        }

    }
}
