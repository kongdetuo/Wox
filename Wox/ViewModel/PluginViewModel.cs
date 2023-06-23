
using System.Reactive.Linq;
using System;
using Wox.Plugin;
using Wox.Core.Resource;
using Wox.Image;
using Wox.Infrastructure.UserSettings;
using System.Diagnostics;
using Avalonia.Media;
using ReactiveUI;
using Avalonia.Layout;

namespace Wox.ViewModel
{
    public class PluginViewModel : BaseModel
    {

        public PluginViewModel(PluginProxy plugin)
        {
            this.PluginPair = plugin;

            this.Disabled = plugin.Metadata.Disabled;

            this.WhenAnyValue(p => p.Disabled).Subscribe(p =>
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

        public IImage Image => ImageLoader.Load(Metadata.IcoPath, Metadata.PluginDirectory);
        public bool ActionKeywordsVisibility => Metadata.ActionKeywords.Count == 1;
        public string InitilizaTime => string.Format(_translator.GetTranslation("plugin_init_time"), Metadata.InitTime);
        public string QueryTime => string.Format(_translator.GetTranslation("plugin_query_time"), Metadata.AvgQueryTime);
        public string ActionKeywordsText => string.Join(Query.ActionKeywordSeperater, Metadata.ActionKeywords);

        public Avalonia.Controls. Control SettingProvider
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
                    return new Avalonia.Controls.Control();
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
