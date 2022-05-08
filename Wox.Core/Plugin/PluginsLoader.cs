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

namespace Wox.Core.Plugin
{
    public static class PluginsLoader
    {
        public const string PATH = "PATH";
        public const string Python = "python";
        public const string PythonExecutable = "pythonw.exe";
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static List<PluginProxy> Plugins(List<PluginMetadata> metadatas, PluginsSettings settings)
        {
            var csharpPlugins = CSharpPlugins(metadatas).ToList();
            var pythonPlugins = PythonPlugins(metadatas, settings.PythonDirectory);
            var executablePlugins = ExecutablePlugins(metadatas);
            var plugins = csharpPlugins.Concat(pythonPlugins).Concat(executablePlugins);
            return plugins
                .OrderBy(p => p.Metadata.Name, StringComparer.InvariantCulture)
                .ToList();
        }

        public static IEnumerable<PluginProxy> CSharpPlugins(List<PluginMetadata> source)
        {
            var metadatas = source
                .Where(IsCSharpPlugin);

            return metadatas//.AsParallel()
                .Select(LoadCSharpPlugin)
                .Where(p => p != null)
                .ToList();
        }

        private static PluginProxy LoadCSharpPlugin(PluginMetadata metadata)
        {
            PluginProxy pair = null;
            var milliseconds = Logger.StopWatchDebug($"Constructor init cost for {metadata.Name}", () =>
            {
                Assembly assembly;
                try
                {
                    var context = new CsharpPluginAssemblyLoadContext(metadata.ExecuteFilePath);
                    assembly = context.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(metadata.ExecuteFilePath)));
                }
                catch (Exception e)
                {
                    e.Data.Add(nameof(metadata.ID), metadata.ID);
                    e.Data.Add(nameof(metadata.Name), metadata.Name);
                    e.Data.Add(nameof(metadata.PluginDirectory), metadata.PluginDirectory);
                    e.Data.Add(nameof(metadata.Website), metadata.Website);
                    Logger.WoxError($"Couldn't load assembly for {metadata.Name}", e);
                    return;
                }
                Type type;
                try
                {
                    var types = assembly.GetExportedTypes();
                    type = types.FirstOrDefault(type => typeof(IPlugin).IsAssignableFrom(type));
                    if (type is null)
                        return;
                }
                catch (InvalidOperationException e)
                {
                    e.Data.Add(nameof(metadata.ID), metadata.ID);
                    e.Data.Add(nameof(metadata.Name), metadata.Name);
                    e.Data.Add(nameof(metadata.PluginDirectory), metadata.PluginDirectory);
                    e.Data.Add(nameof(metadata.Website), metadata.Website);
                    Logger.WoxError($"Can't find class implement IPlugin for <{metadata.Name}>", e);
                    return;
                }
                IPlugin plugin;
                try
                {
                    plugin = (IPlugin)Activator.CreateInstance(type);
                }
                catch (Exception e)
                {
                    e.Data.Add(nameof(metadata.ID), metadata.ID);
                    e.Data.Add(nameof(metadata.Name), metadata.Name);
                    e.Data.Add(nameof(metadata.PluginDirectory), metadata.PluginDirectory);
                    e.Data.Add(nameof(metadata.Website), metadata.Website);
                    Logger.WoxError($"Can't create instance for <{metadata.Name}>", e);
                    return;
                }

                pair = new PluginProxy
                {
                    Plugin = plugin,
                    Metadata = metadata
                };
            });
            metadata.InitTime += milliseconds;
            return pair;
        }

        public static IEnumerable<PluginProxy> PythonPlugins(List<PluginMetadata> source, string pythonDirecotry)
        {
            var metadatas = source.Where(o => o.Language.ToUpper() == AllowedLanguage.Python);
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
            var plugins = metadatas.Select(metadata => new PluginProxy
            {
                Plugin = new PythonPlugin(filename),
                Metadata = metadata
            });
            return plugins;
        }

        public static IEnumerable<PluginProxy> ExecutablePlugins(IEnumerable<PluginMetadata> source)
        {
            var metadatas = source.Where(o => o.Language.ToUpper() == AllowedLanguage.Executable);

            var plugins = metadatas.Select(metadata => new PluginProxy
            {
                Plugin = new ExecutablePlugin(metadata.ExecuteFilePath),
                Metadata = metadata
            });
            return plugins;
        }

        private static bool IsCSharpPlugin(PluginMetadata metadata)
            => string.Equals(metadata.Language, AllowedLanguage.CSharp, StringComparison.OrdinalIgnoreCase);

        private static bool IsPythonPlugin(PluginMetadata metadata)
            => string.Equals(metadata.Language, AllowedLanguage.Python, StringComparison.OrdinalIgnoreCase);
    }
}