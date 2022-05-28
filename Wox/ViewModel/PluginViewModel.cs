using System.Windows;
using System.Windows.Media;
using Wox.Plugin;
using Wox.Core.Resource;
using Wox.Image;
using System.Windows.Controls;

namespace Wox.ViewModel
{
    public class PluginViewModel : BaseModel
    {
        public PluginProxy PluginPair { get; set; }

        private readonly Internationalization _translator = InternationalizationManager.Instance;

        public bool Enabled { get; set; }

        public ImageSource Image => ImageLoader.Load(PluginPair.Metadata.IcoPath, PluginPair.Metadata.PluginDirectory);
        public Visibility ActionKeywordsVisibility => PluginPair.Metadata.ActionKeywords.Count > 1 ? Visibility.Collapsed : Visibility.Visible;
        public string InitilizaTime => string.Format(_translator.GetTranslation("plugin_init_time"), PluginPair.Metadata.InitTime);
        public string QueryTime => string.Format(_translator.GetTranslation("plugin_query_time"), PluginPair.Metadata.AvgQueryTime);
        public string ActionKeywordsText => string.Join(Query.ActionKeywordSeperater, PluginPair.Metadata.ActionKeywords);

        public Control SettingProvider
        {
            get
            {
                var settingProvider = PluginPair.Plugin as ISettingProvider;
                if (settingProvider != null)
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

    }
}
