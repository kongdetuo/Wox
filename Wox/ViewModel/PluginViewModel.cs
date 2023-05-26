using System.Windows;
using System.Windows.Media;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System;
using Wox.Plugin;
using Wox.Core.Resource;
using Wox.Image;
using System.Windows.Controls;
using Wox.Infrastructure.UserSettings;
using System.Reactive.Concurrency;
using System.Diagnostics;

namespace Wox.ViewModel
{
    public class PluginViewModel : BaseModel
    {

        public PluginViewModel(PluginProxy plugin)
        {
            this.PluginPair = plugin;

            this.Disabled = plugin.Metadata.Disabled;

            this.WhenChanged(p => p.Disabled).Subscribe(p =>
            {
                // used to sync the current status from the plugin manager into the setting to keep consistency after save
                plugin.Metadata.Disabled = p;
                Settings.Instance.PluginSettings.Plugins[plugin.Metadata.ID].Disabled = p;
            });
        }
        public PluginProxy PluginPair { get; set; }

        public PluginMetadata Metadata => PluginPair.Metadata;

        private readonly Internationalization _translator = InternationalizationManager.Instance;

        public bool Enabled { get; set; }

        public bool Disabled { get; set; }

        public ImageSource Image => ImageLoader.Load(Metadata.IcoPath, Metadata.PluginDirectory);
        public Visibility ActionKeywordsVisibility => Metadata.ActionKeywords.Count > 1 ? Visibility.Collapsed : Visibility.Visible;
        public string InitilizaTime => string.Format(_translator.GetTranslation("plugin_init_time"), Metadata.InitTime);
        public string QueryTime => string.Format(_translator.GetTranslation("plugin_query_time"), Metadata.AvgQueryTime);
        public string ActionKeywordsText => string.Join(Query.ActionKeywordSeperater, Metadata.ActionKeywords);

        public Control SettingProvider
        {
            get
            {
                if (PluginPair.Plugin is ISettingProvider settingProvider)
                {
                    var control = settingProvider.CreateSettingPanel();
                    control.HorizontalAlignment = HorizontalAlignment.Stretch;
                    control.VerticalAlignment = VerticalAlignment.Stretch;
                    return control;
                }
                else
                {
                    return new Control();
                }
            }
        }

        private RelayCommand openDirectoryCommand = null!;
        public RelayCommand OpenDirectoryCommand => openDirectoryCommand ??= new RelayCommand(p =>
        {
            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = this.Metadata.PluginDirectory,
                    UseShellExecute = true
                });
            }
            catch
            {
            }
        });


    }
}
