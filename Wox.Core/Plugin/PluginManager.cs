using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Wox.Infrastructure;
using Wox.Infrastructure.Logger;
using Wox.Infrastructure.Storage;
using Wox.Infrastructure.UserSettings;
using Wox.Plugin;

namespace Wox.Core.Plugin
{
    /// <summary>
    /// The entry for managing Wox plugins
    /// </summary>
    public static class PluginManager
    {
        private static Dictionary<string, PluginProxy> PluginDic;
        public static List<PluginProxy> AllPlugins { get; private set; }

        public static IPublicAPI API { private set; get; }

        // todo this should not be public, the indicator function should be embeded
        public static PluginsSettings Settings;
        private static List<PluginMetadata> _metadatas;
        private static readonly string[] Directories = { Constant.PreinstalledDirectory, DataLocation.PluginsDirectory };

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static void ValidateUserDirectory()
        {
            if (!Directory.Exists(DataLocation.PluginsDirectory))
            {
                Directory.CreateDirectory(DataLocation.PluginsDirectory);
            }
        }

        private static void DeletePythonBinding()
        {
            const string binding = "wox.py";
            var directory = DataLocation.PluginsDirectory;
            foreach (var subDirectory in Directory.GetDirectories(directory))
            {
                var path = Path.Combine(subDirectory, binding);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        public static void Save()
        {
            foreach (var plugin in AllPlugins)
            {
                var savable = plugin.Plugin as ISavable;
                savable?.Save();
            }
        }

        public static void ReloadData()
        {
            foreach (var plugin in AllPlugins)
            {
                var reloadablePlugin = plugin.Plugin as IReloadable;
                reloadablePlugin?.ReloadData();
            }
        }

        static PluginManager()
        {
            ValidateUserDirectory();
            // force old plugins use new python binding
            DeletePythonBinding();
        }

        /// <summary>
        /// because InitializePlugins needs API, so LoadPlugins needs to be called first
        /// todo The API should be removed
        /// </summary>
        /// <param name="settings"></param>
        public static void LoadPlugins(PluginsSettings settings)
        {
            _metadatas = PluginConfig.Parse(Directories);
            Settings = settings;
            Settings.UpdatePluginSettings(_metadatas);
            AllPlugins = PluginsLoader.Plugins(_metadatas, Settings);
            PluginDic = AllPlugins.ToDictionary(p => p.Metadata.ID);
        }

        /// <summary>
        /// Call initialize for all plugins
        /// </summary>
        /// <returns>return the list of failed to init plugins or null for none</returns>
        public static async Task InitializePluginsAsync(IPublicAPI api)
        {
            API = api;
            var failedPlugins = new ConcurrentQueue<PluginProxy>();

            await Task.WhenAll(AllPlugins.AsParallel().Select(async pair =>
            {
                try
                {
                    var milliseconds = await Logger.StopWatchDebugAsync($"Init method time cost for <{pair.Metadata.Name}>",
                        () => pair.InitAsync(new PluginInitContext(pair.Metadata, api)));
                    pair.Metadata.InitTime += milliseconds;
                    Logger.WoxInfo($"Total init cost for <{pair.Metadata.Name}> is <{pair.Metadata.InitTime}ms>");
                }
                catch (Exception e)
                {
                    e.Data.Add(nameof(pair.Metadata.ID), pair.Metadata.ID);
                    e.Data.Add(nameof(pair.Metadata.Name), pair.Metadata.Name);
                    e.Data.Add(nameof(pair.Metadata.PluginDirectory), pair.Metadata.PluginDirectory);
                    e.Data.Add(nameof(pair.Metadata.Website), pair.Metadata.Website);
                    Logger.WoxError($"Fail to Init plugin: {pair.Metadata.Name}", e);
                    pair.Metadata.Disabled = true;
                    failedPlugins.Enqueue(pair);
                }
            }));

            if (failedPlugins.Any())
            {
                var failed = string.Join(",", failedPlugins.Select(x => x.Metadata.Name));
                API.ShowMsg($"Fail to Init Plugins", $"Plugins: {failed} - fail to load and would be disabled, please contact plugin creator for help", "", false);
            }
        }

        public static void InstallPlugin(string path)
        {
            PluginInstaller.Install(path);
        }

        /// <summary>
        /// get specified plugin, return null if not found
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static PluginProxy GetPluginForId(string id)
        {
            if (id != null && PluginDic.TryGetValue(id, out var plugin))
            {
                return plugin;
            }
            return null;
        }

        public static IEnumerable<PluginProxy> GetPluginsForInterface<T>() where T : IFeatures
        {
            return AllPlugins.Where(p => p.Plugin is T);
        }

        public static async Task<List<Result>> GetContextMenusForPlugin(Result result)
        {
            var pluginPair = GetPluginForId(result.PluginID);
            if (pluginPair != null)
            {
                var metadata = pluginPair.Metadata;

                try
                {
                    List<Result> results = new();
                    if (pluginPair.Plugin is IAsyncContextMenu asyncContext)
                    {
                        results = await asyncContext.LoadContextMenusAsync(result);
                    }
                    else if (pluginPair.Plugin is IContextMenu context)
                    {
                        results = context.LoadContextMenus(result);
                    }

                    foreach (var r in results)
                    {
                        r.PluginID = metadata.ID;
                        r.OriginQuery = result.OriginQuery;
                    }
                    return results;
                }
                catch (Exception e)
                {
                    Logger.WoxError($"Can't load context menus for plugin <{metadata.Name}>", e);
                    return new List<Result>();
                }
            }
            else
            {
                return new List<Result>();
            }

        }

        /// <summary>
        /// used to add action keyword for multiple action keyword plugin
        /// e.g. web search
        /// </summary>
        public static void AddActionKeyword(string id, string newActionKeyword)
        {
            AddActionKeyword(id, new Keyword(newActionKeyword));
        }

        /// <summary>
        /// used to add action keyword for multiple action keyword plugin
        /// e.g. web search
        /// </summary>
        public static void AddActionKeyword(string id, Keyword newActionKeyword)
        {
            var plugin = GetPluginForId(id);
            plugin.Metadata.ActionKeywords.Add(newActionKeyword);
        }
        /// <summary>
        /// used to add action keyword for multiple action keyword plugin
        /// e.g. web search
        /// </summary>
        public static void RemoveActionKeyword(string id, string oldActionkeyword)
        {
            RemoveActionKeyword(id, new Keyword(oldActionkeyword));
        }

        /// <summary>
        /// used to add action keyword for multiple action keyword plugin
        /// e.g. web search
        /// </summary>
        public static void RemoveActionKeyword(string id, Keyword oldActionkeyword)
        {
            var plugin = GetPluginForId(id);

            plugin.Metadata.ActionKeywords.Remove(oldActionkeyword);
        }

        public static void ReplaceActionKeyword(string id, string oldActionKeyword, string newActionKeyword)
        {
            if (oldActionKeyword != newActionKeyword)
            {
                AddActionKeyword(id, newActionKeyword);
                RemoveActionKeyword(id, oldActionKeyword);
            }
        }

        public static void ReplaceActionKeyword(string id, Keyword oldActionKeyword, Keyword newActionKeyword)
        {
            if (oldActionKeyword != newActionKeyword)
            {
                RemoveActionKeyword(id, oldActionKeyword);
                AddActionKeyword(id, newActionKeyword);
            }
        }
    }

    public class HistoryPlugin : IPlugin
    {
        public void Init(PluginInitContext context)
        {
            throw new NotImplementedException();
        }

        public List<Result> Query(Query query)
        {
            throw new NotImplementedException();
        }
    }
}
