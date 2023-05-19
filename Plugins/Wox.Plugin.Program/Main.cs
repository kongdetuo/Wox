using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using NLog;
using Wox.Infrastructure;
using Wox.Infrastructure.Logger;
using Wox.Infrastructure.Storage;
using Wox.Plugin.Program.Programs;
using Wox.Plugin.Program.Views;

namespace Wox.Plugin.Program
{
    public class Main : ISettingProvider, IPlugin, IPluginI18n, IContextMenu, ISavable, IReloadable
    {
        internal static Win32[] Win32s { get; set; }
        internal static UWP.Application[] _uwps { get; set; }
        internal static Settings Settings { get; set; }

        private static PluginInitContext _context;

        private PluginJsonStorage<Settings> _settingsStorage;

        private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

        private static void preloadPrograms()
        {

        }

        public void Save()
        {
            _settingsStorage.Save();
        }

        public List<Result> Query(Query query)
        {
            return Query(query.RawQuery);
        }

        public static List<Result> Query(string query)
        {
            return Enumerable.Empty<IProgram>().Concat(Win32s).Concat(_uwps)
                .Where(p => p.Enabled)
                .Select(p => p.Result(query, _context.API))
                .Where(p => p.Score > 0)
                .Where(p => !NeedIgnore(p))
                .OrderByDescending(p => p.Score)
                .ToList();

            bool NeedIgnore(Result r)
            {
                var ignored = Settings.IgnoredSequence.Any(entry =>
                {
                    if (entry.IsRegex)
                    {
                        return Regex.Match(r.Title.Text, entry.EntryString).Success || Regex.Match(r.SubTitle.Text, entry.EntryString).Success;
                    }
                    else
                    {
                        return r.Title.Text.ToLower().Contains(entry.EntryString) || r.SubTitle.Text.ToLower().Contains(entry.EntryString);
                    }
                });
                return ignored;
            }
        }
        public void Init(PluginInitContext context)
        {
            _context = context;
            loadSettings();

            preloadPrograms();

            _ = Task.Delay(2000).ContinueWith(_ =>
            {
                IndexPrograms();
                Save();
            });
        }

        public void InitSync(PluginInitContext context)
        {
            _context = context;
            loadSettings();
            IndexPrograms();
        }

        public void loadSettings()
        {
            _settingsStorage = new PluginJsonStorage<Settings>();
            Settings = _settingsStorage.Load();
        }

        public static void IndexWin32Programs()
        {
            var win32S = Win32.All(Settings);
            Win32s = win32S;
        }

        public static void IndexUWPPrograms()
        {
            var windows10 = new Version(10, 0);
            var support = Environment.OSVersion.Version.Major >= windows10.Major;

            var applications = support ? UWP.All() : Array.Empty<UWP.Application>();
            _uwps = applications;
        }

        public static void IndexPrograms()
        {
            var a = Task.Run(() =>
            {
                Logger.StopWatchNormal("Win32 index cost", IndexWin32Programs);
            });

            var b = Task.Run(() =>
            {
                Logger.StopWatchNormal("UWP index cost", IndexUWPPrograms);
            });

            Task.WaitAll(a, b);

            Logger.WoxInfo($"Number of indexed win32 programs <{Win32s.Length}>");
            foreach (var win32 in Win32s)
            {
                Logger.WoxDebug($" win32: <{win32.Name}> <{win32.ExecutableName}> <{win32.FullPath}>");
            }
            Logger.WoxInfo($"Number of indexed uwps <{_uwps.Length}>");
            foreach (var uwp in _uwps)
            {
                Logger.WoxDebug($" uwp: <{uwp.DisplayName}> <{uwp.UserModelId}>");
            }
            Settings.LastIndexTime = DateTime.Today;

        }

        public Control CreateSettingPanel()
        {
            return new ProgramSetting(_context, Settings);
        }

        public string GetTranslatedPluginTitle()
        {
            return _context.API.GetTranslation("wox_plugin_program_plugin_name");
        }

        public string GetTranslatedPluginDescription()
        {
            return _context.API.GetTranslation("wox_plugin_program_plugin_description");
        }

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            var menuOptions = new List<Result>();
            if (selectedResult.ContextData is IProgram program)
            {
                menuOptions = program.ContextMenus(_context.API);
            }
            return menuOptions;
        }

        public void ReloadData()
        {
            IndexPrograms();
        }
    }
}
