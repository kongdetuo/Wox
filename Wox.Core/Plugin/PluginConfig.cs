using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;
using Wox.Infrastructure.Exception;
using Wox.Infrastructure.Logger;
using Wox.Plugin;
using Wox.Infrastructure.Storage;
using System.Windows.Automation;

namespace Wox.Core.Plugin
{

    [JsonObject(MemberSerialization.OptOut)]
    public class PluginConfig
    {
        internal const string PluginConfigName = "plugin.json";
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public string ID { get; set; }
        public string Name { get; set; }
        public string Author { get; set; }
        public string Version { get; set; }
        public string Language { get; set; }
        public string Description { get; set; }
        public string Website { get; set; }

        public string GetExecuteFilePath()
        {
            if (string.IsNullOrEmpty(ExecuteFileName))
                return string.Empty;
            if (File.Exists(ExecuteFileName))
                return ExecuteFileName;

            var path = Path.Join(PluginDirectory, ExecuteFileName);
            if (File.Exists(path))
                return path;
            return string.Empty;
        }

        public string ExecuteFileName { get; set; }

        public string PluginDirectory { get; set; }

        public Keyword ActionKeyword { get; set; }

        public List<Keyword> ActionKeywords { get; set; }

        public string IcoPath { get; set; }
        public bool KeepResultRawScore { get; set; }

        /// <summary>
        /// Parse plugin metadata in giving directories
        /// </summary>
        /// <param name="pluginDirectories"></param>
        /// <returns></returns>
        public static List<PluginConfig> Parse(string[] pluginDirectories)
        {
            var directories = pluginDirectories.SelectMany(Directory.GetDirectories);
            return ParsePluginConfigs(directories).Where(p => p != null).ToList();
        }

        private static List<PluginConfig> ParsePluginConfigs(IEnumerable<string> directories)
        {
            var list = new List<PluginConfig>();
            // todo use linq when diable plugin is implmented since parallel.foreach + list is not thread saft
            foreach (var directory in directories)
            {
                if (File.Exists(Path.Combine(directory, "NeedDelete.txt")))
                {
                    try
                    {
                        Directory.Delete(directory, true);
                    }
                    catch (Exception e)
                    {
                        Logger.WoxError($"Can't delete <{directory}>", e);
                    }
                }
                else
                {
                    PluginConfig metadata = Load(directory);
                    if (metadata != null)
                    {
                        list.Add(metadata);
                    }
                }
            }
            return list;
        }

        public static PluginConfig Load(string pluginDirectory)
        {
            string configPath = Path.Combine(pluginDirectory, PluginConfigName);
            if (!File.Exists(configPath))
            {
                Logger.WoxError($"Didn't find config file <{configPath}>");
                return null;
            }

            PluginConfig config;
            try
            {
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new KeywordConvertor());

                config = JsonConvert.DeserializeObject<PluginConfig>(File.ReadAllText(configPath), settings);
                config.PluginDirectory = pluginDirectory;

            }
            catch (Exception e)
            {
                e.Data.Add(nameof(configPath), configPath);
                e.Data.Add(nameof(pluginDirectory), pluginDirectory);
                Logger.WoxError($"invalid json for config <{configPath}>", e);
                return null;
            }


            if (!AllowedLanguage.IsAllowed(config.Language))
            {
                Logger.WoxError($"Invalid language <{config.Language}> for config <{configPath}>");
                return null;
            }

            if (!File.Exists(config.GetExecuteFilePath()))
            {
                Logger.WoxError($"execute file path didn't exist <{config.GetExecuteFilePath()}> for conifg <{configPath}");
                return null;
            }

            return config;
        }
    }
}