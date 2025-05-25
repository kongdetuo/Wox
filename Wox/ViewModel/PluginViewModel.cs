
using Avalonia.Layout;
using Avalonia.Media;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using Wox.Core.Plugin;
using Wox.Core.Resource;
using Wox.Image;
using Wox.Infrastructure.UserSettings;
using Wox.Plugin;

namespace Wox.ViewModel
{
    public class PluginViewModel : BaseModel
    {

        public PluginViewModel(WoxPlugin plugin)
        {
            this.PluginPair = plugin;

            this.Disabled = plugin.Metadata.Disabled;

            this.Options = plugin.Instance.Options.ToList();

            this.WhenAnyValue(p => p.Disabled).Subscribe(p =>
            {
                // used to sync the current status from the plugin manager into the setting to keep consistency after save
                plugin.Metadata.Disabled = p;
                Settings.Instance.PluginSettings.Plugins[plugin.Metadata.ID].Disabled = p;
            });
        }
        public WoxPlugin PluginPair { get; set; }

        public PluginMetadata Metadata => PluginPair.Metadata;

        private readonly Internationalization _translator = InternationalizationManager.Instance;

        public bool Enabled { get; set; }

        public bool Disabled { get; set; }

        public IImage Image => ImageLoader.Load(Metadata.IcoPath, Metadata.PluginDirectory);
        public bool ActionKeywordsVisibility => Metadata.ActionKeywords.Count == 1;
        public string InitilizaTime => string.Format(_translator.GetTranslation("plugin_init_time"), Metadata.InitTime);
        public string QueryTime => string.Format(_translator.GetTranslation("plugin_query_time"), Metadata.AvgQueryTime);
        public string ActionKeywordsText => string.Join(Query.ActionKeywordSeperater, Metadata.ActionKeywords);

        public System.Collections.Generic.List<PluginOption> Options { get; set; }


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


        public void SaveOptions()
        {
            foreach (var option in Options)
            {
                option.Save();
            }

            this.PluginPair.Instance.SaveOptions(this.Options);
        }

    }
}
