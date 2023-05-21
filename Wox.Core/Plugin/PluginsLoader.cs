using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NLog;
using Wox.Infrastructure;
using Wox.Infrastructure.Exception;
using Wox.Infrastructure.Logger;
using Wox.Infrastructure.UserSettings;
using Wox.Plugin;
using System.Xml.Linq;
using Wox.Core.Resource;

namespace Wox.Core.Plugin
{
    public static class PluginsLoader
    {
        public const string PATH = "PATH";
        public const string Python = "python";
        public const string PythonExecutable = "pythonw.exe";
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static List<PluginProxy> Plugins(List<PluginConfig> metadatas, PluginsSettings settings)
        {
            var csharpPlugins = CSharpPlugins(metadatas).ToList();
            var pythonPlugins = PythonPlugins(metadatas, settings.PythonDirectory);
            var executablePlugins = ExecutablePlugins(metadatas);
            var plugins = csharpPlugins.Concat(pythonPlugins).Concat(executablePlugins);
            return plugins
                .OrderBy(p => p.Metadata.Name, StringComparer.InvariantCulture)
                .ToList();
        }

        public static IEnumerable<PluginProxy> CSharpPlugins(List<PluginConfig> source)
        {
            var metadatas = source
                .Where(IsCSharpPlugin);

            return metadatas//.AsParallel()
                .Select(LoadCSharpPlugin)
                .Where(p => p != null)
                .ToList();
        }

        private static PluginProxy LoadCSharpPlugin(PluginConfig config)
        {
            PluginProxy pair = null;
            var milliseconds = Logger.StopWatchDebug($"Constructor init cost for {config.Name}", () =>
            {
                Assembly assembly;
                try
                {
                    var context = new CsharpPluginAssemblyLoadContext(config.GetExecuteFilePath());
                    assembly = context.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(config.GetExecuteFilePath())));
                }
                catch (Exception e)
                {
                    e.Data.Add(nameof(config.ID), config.ID);
                    e.Data.Add(nameof(config.Name), config.Name);
                    e.Data.Add(nameof(config.PluginDirectory), config.PluginDirectory);
                    e.Data.Add(nameof(config.Website), config.Website);
                    Logger.WoxError($"Couldn't load assembly for {config.Name}", e);
                    return;
                }
                Type type;
                try
                {
                    var types = assembly.GetExportedTypes();
                    type = types.FirstOrDefault(type => typeof(IAsyncPlugin).IsAssignableFrom(type));
                    if (type is null)
                        return;
                }
                catch (InvalidOperationException e)
                {
                    e.Data.Add(nameof(config.ID), config.ID);
                    e.Data.Add(nameof(config.Name), config.Name);
                    e.Data.Add(nameof(config.PluginDirectory), config.PluginDirectory);
                    e.Data.Add(nameof(config.Website), config.Website);
                    Logger.WoxError($"Can't find class implement IPlugin for <{config.Name}>", e);
                    return;
                }
                IAsyncPlugin plugin;
                try
                {
                    plugin = (IAsyncPlugin)Activator.CreateInstance(type);
                }
                catch (Exception e)
                {
                    e.Data.Add(nameof(config.ID), config.ID);
                    e.Data.Add(nameof(config.Name), config.Name);
                    e.Data.Add(nameof(config.PluginDirectory), config.PluginDirectory);
                    e.Data.Add(nameof(config.Website), config.Website);
                    Logger.WoxError($"Can't create instance for <{config.Name}>", e);
                    return;
                }

                pair = new PluginProxy
                {
                    Plugin = plugin,
                    Metadata = CreateMetadata(config)
                };
            });
            if (pair == null)
                return null;
            pair.Metadata.InitTime += milliseconds;
            return pair;
        }

        private static IEnumerable<PluginProxy> PythonPlugins(List<PluginConfig> source, string pythonDirecotry)
        {
            var configs = source.Where(o => o.Language.ToUpper() == AllowedLanguage.Python);
            string filename;

            if (string.IsNullOrEmpty(pythonDirecotry))
            {
                var paths = Environment.GetEnvironmentVariable(PATH);
                if (paths != null)
                {
                    var pythonPaths = paths.Split(';').Where(p => p.ToLower().Contains(Python));
                    if (pythonPaths.Any())
                    {
                        filename = PythonExecutable;
                    }
                    else
                    {
                        Logger.WoxError("Python can't be found in PATH.");
                        return new List<PluginProxy>();
                    }
                }
                else
                {
                    Logger.WoxError("PATH environment variable is not set.");
                    return new List<PluginProxy>();
                }
            }
            else
            {
                var path = Path.Combine(pythonDirecotry, PythonExecutable);
                if (File.Exists(path))
                {
                    filename = path;
                }
                else
                {
                    Logger.WoxError("Can't find python executable in <b ");
                    return new List<PluginProxy>();
                }
            }
            Constant.PythonPath = filename;
            var plugins = configs.Select(config => new PluginProxy
            {
                Plugin = new PythonPlugin(filename),
                Metadata = CreateMetadata(config)
            });
            return plugins;
        }

        private static IEnumerable<PluginProxy> ExecutablePlugins(IEnumerable<PluginConfig> source)
        {
            var configs = source.Where(o => o.Language.ToUpper() == AllowedLanguage.Executable);

            var plugins = configs.Select(config => new PluginProxy
            {
                Plugin = new ExecutablePlugin(config.GetExecuteFilePath()),
                Metadata = CreateMetadata(config)
            });
            return plugins;
        }

        private static bool IsCSharpPlugin(PluginConfig metadata)
            => string.Equals(metadata.Language, AllowedLanguage.CSharp, StringComparison.OrdinalIgnoreCase);

        private static bool IsPythonPlugin(PluginConfig metadata)
            => string.Equals(metadata.Language, AllowedLanguage.Python, StringComparison.OrdinalIgnoreCase);

        public static PluginMetadata CreateMetadata(PluginConfig config)
        {
            var m = new PluginMetadata()
            {
                ID = config.ID,
                Language = config.Language ?? string.Empty,
                PluginDirectory = config.PluginDirectory,
                ExecuteFilePath = config.GetExecuteFilePath(),
                ActionKeywords = (config.ActionKeywords ?? new()).Append(config.ActionKeyword).Distinct().Where(p => p.IsEmpty == false).ToList(),

                Name = string.IsNullOrEmpty(config.Name) ? "无名插件" : config.Name,
                Description = config.Description ?? string.Empty,
                Author = config.Author ?? string.Empty,
                Version = config.Version ?? string.Empty,
                Website = config.Website ?? string.Empty,
                IcoPath = string.IsNullOrEmpty(config.IcoPath) ? "" : Path.Join(config.PluginDirectory, config.IcoPath),

                Disabled = config.Disabled,
                KeepResultRawScore = config.KeepResultRawScore
            };
            return m;
        }
    }
}