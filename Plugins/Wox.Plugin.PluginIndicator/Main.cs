using System.Collections.Generic;
using System.Linq;
using Wox.Core.Plugin;

namespace Wox.Plugin.PluginIndicator
{
    public class Main : IPlugin, IPluginI18n
    {
        private PluginInitContext context = null!;

        public List<Result> Query(Query query)
        {
            var results = from plugin in PluginManager.AllPlugins
                          where plugin.Metadata.Disabled == false
                          from keyword in plugin.Metadata.ActionKeywords
                          where keyword.Key.StartsWith(query.Terms[0])
                          select new Result
                          {
                              Title = keyword.Key,
                              SubTitle = $"Activate {plugin.Metadata.Name} plugin",
                              Score = 100,
                              IcoPath = plugin.Metadata.IcoPath,
                              Action = c =>
                              {
                                  context.API.ChangeQuery($"{keyword}{Plugin.Query.TermSeperater}");
                                  return false;
                              }
                          };
            return results.ToList();
        }

        public void Init(PluginInitContext context)
        {
            this.context = context;
        }

        public string GetTranslatedPluginTitle()
        {
            return context.API.GetTranslation("wox_plugin_pluginindicator_plugin_name");
        }

        public string GetTranslatedPluginDescription()
        {
            return context.API.GetTranslation("wox_plugin_pluginindicator_plugin_description");
        }
    }
}
