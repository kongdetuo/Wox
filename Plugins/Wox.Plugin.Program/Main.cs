using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using NLog;
using Wox.Infrastructure;
using Wox.Infrastructure.Logger;
using Wox.Infrastructure.Storage;
using Wox.Plugin.Program.Programs;
using System.Threading;

namespace Wox.Plugin.Program
{
    public class Main : IAsyncPlugin, IPluginI18n,/* IContextMenu,*/ IReloadable
    {
        internal static Win32[] Win32s { get; set; }
        internal static UWP.Application[] _uwps { get; set; }
        internal static Settings Settings { get; set; }

        private static PluginInitContext _context;

        private PluginJsonStorage<Settings> _settingsStorage;

        private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

        public void Save()
        {
            _settingsStorage.Save();
        }

        public async Task<List<IResult>> QueryAsync(Query query, CancellationToken token)
        {
            await Task.Yield();
            return Enumerable.Empty<IProgram>().Concat(Win32s).Concat(_uwps)
                .Where(p => p.Enabled)
                .Select(p => p.Result(query.Search, _context.API))
                .Where(p => p.Score > 0)
                .Where(p => !NeedIgnore(p))
                .OrderByDescending(p => p.Score)
                .OfType<IResult>()
                .ToList();

            bool NeedIgnore(IResult r)
            {
                var ignored = Settings.IgnoredSequence.Any(entry =>
                {
                    if (entry.IsRegex)
                    {
                        return Regex.Match(r.Title, entry.EntryString).Success || Regex.Match(r.SubTitle, entry.EntryString).Success;
                    }
                    else
                    {
                        return r.Title.ToLower().Contains(entry.EntryString) || r.SubTitle.ToLower().Contains(entry.EntryString);
                    }
                });
                return ignored;
            }
        }

        public async Task InitAsync(PluginInitContext context)
        {
            _context = context;
            _settingsStorage = new PluginJsonStorage<Settings>();
            Settings = await _settingsStorage.LoadAsync();
            await IndexPrograms();
            Save();
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

        public static async Task IndexPrograms()
        {
            var a = Task.Run(() =>
            {
                Logger.StopWatchNormal("Win32 index cost", IndexWin32Programs);
            });

            var b = Task.Run(() =>
            {
                Logger.StopWatchNormal("UWP index cost", IndexUWPPrograms);
            });

            await Task.WhenAll(a, b);

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

        public string GetTranslatedPluginTitle()
        {
            return _context.API.GetTranslation("wox_plugin_program_plugin_name");
        }

        public string GetTranslatedPluginDescription()
        {
            return _context.API.GetTranslation("wox_plugin_program_plugin_description");
        }

        public List<Result> LoadContextMenus(Result selectedResult, ActionContext context)
        {
            var menuOptions = new List<Result>();
            if (selectedResult.ContextData is IProgram program)
            {
                menuOptions = program.ContextMenus(context.API);
            }
            return menuOptions;
        }

        public void ReloadData()
        {
            IndexPrograms();
        }

        public IEnumerable<PluginOption> Options =>
            /**
*     <!--Program setting-->
<system:String x:Key="wox_plugin_program_sources">Program Sources</system:String>
<system:String x:Key="wox_plugin_program_delete">删除</system:String>
<system:String x:Key="wox_plugin_program_edit">编辑</system:String>
<system:String x:Key="wox_plugin_program_add">增加</system:String>
<system:String x:Key="wox_plugin_program_location">位置</system:String>
<system:String x:Key="wox_plugin_program_suffixes">索引文件后缀</system:String>
<system:String x:Key="wox_plugin_program_reindex">重新索引</system:String>
<system:String x:Key="wox_plugin_program_indexing">索引中</system:String>
<system:String x:Key="wox_plugin_program_index_start">索引开始菜单</system:String>
<system:String x:Key="wox_plugin_program_index_registry">索引注册表</system:String>
<system:String x:Key="wox_plugin_program_suffixes_header">后缀</system:String>
<system:String x:Key="wox_plugin_program_max_depth_header">最大深度</system:String>

<system:String x:Key="wox_plugin_program_directory">目录</system:String>
<system:String x:Key="wox_plugin_program_browse">浏览</system:String>
<system:String x:Key="wox_plugin_program_file_suffixes">文件后缀</system:String>
<system:String x:Key="wox_plugin_program_max_search_depth">最大搜索深度（-1是无限的）：</system:String>

<system:String x:Key="wox_plugin_program_pls_select_program_source">请先选择一项</system:String>

<system:String x:Key="wox_plugin_program_update">更新</system:String>
<system:String x:Key="wox_plugin_program_only_index_tip">Wox仅索引下列后缀的文件：</system:String>
<system:String x:Key="wox_plugin_program_split_by_tip">（每个后缀以英文状态下的分号分隔）</system:String>
<system:String x:Key="wox_plugin_program_update_file_suffixes">成功更新索引文件后缀</system:String>
<system:String x:Key="wox_plugin_program_suffixes_cannot_empty">文件后缀不能为空</system:String>

<system:String x:Key="wox_plugin_program_run_as_different_user">以其他用户身份运行</system:String>
<system:String x:Key="wox_plugin_program_run_as_administrator">以管理员身份运行</system:String>
<system:String x:Key="wox_plugin_program_open_containing_folder">打开所属文件夹</system:String>

<system:String x:Key="wox_plugin_program_plugin_name">程序</system:String>
<system:String x:Key="wox_plugin_program_plugin_description">在Wox中搜索程序</system:String>

<system:String x:Key="wox_plugin_program_invalid_path">无效路径</system:String>
<system:String x:Key="wox_plugin_program_ignored_sequences">Ignored sequences</system:String>
<system:String x:Key="wox_plugin_program_sequence">Sequence</system:String>
<system:String x:Key="wox_plugin_program_pls_select_ignored">Please select an ignored sequence</system:String>
<system:String x:Key="wox_plugin_program_delete_ignored">是否删除 {0}?</system:String>
<system:String x:Key="wox_plugin_program_use_regex">是否使用正则表达式 ?</system:String>

*/
            // 是否索引开始菜单
            // 是否索引注册表
            // 忽略的项目
            // 扫描的文件夹
            // 感觉可以先删掉，不搞这么多配置
            [

            ];

        public void SaveOptions(IEnumerable<PluginOption> options)
        {
            Save();
        }
    }
}
