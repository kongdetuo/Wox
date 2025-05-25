using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Wox.Infrastructure;
using Wox.Infrastructure.Logger;
using Wox.Infrastructure.Storage;
using Wox.Plugin.Everything.Everything;

namespace Wox.Plugin.Everything
{
    public class Main : IAsyncPlugin, IPluginI18n
    {
        public const string DLL = "Everything.dll";
        private readonly EverythingApi _api = new();

        private PluginInitContext _context = null!;

        private Settings _settings = null!;
        private PluginJsonStorage<Settings> _storage = null!;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public async Task<List<IResult>> QueryAsync(Query query, CancellationToken token)
        {
            await Task.Yield();
            var results = new List<IResult>();
            if (!string.IsNullOrEmpty(query.Search))
            {
                var keyword = query.Search;

                try
                {
                    if (token.IsCancellationRequested) { return results; }
                    var searchList = _api.Search(keyword, token, _settings.MaxSearchCount);
                    if (token.IsCancellationRequested) { return results; }
                    for (int i = 0; i < searchList.Count; i++)
                    {
                        if (token.IsCancellationRequested) { return results; }
                        SearchResult searchResult = searchList[i];
                        var r = CreateResult(keyword, searchResult, i);
                        if (r != null)
                        {
                            results.Add(r);
                        }
                    }
                }
                catch (IPCErrorException)
                {
                    results.Add(new Result
                    {
                        Title = _context.API.GetTranslation("wox_plugin_everything_is_not_running"),
                        IcoPath = "Images\\warning.png"
                    });
                }
                catch (Exception e)
                {
                    Logger.WoxError("Query Error", e);
                    results.Add(new Result
                    {
                        Title = _context.API.GetTranslation("wox_plugin_everything_query_error"),
                        SubTitle = e.Message,
                        AsyncAction = async _ =>
                        {
                            await _context.API.Clipboard.SetTextAsync(e.Message + "\r\n" + e.StackTrace);
                            _context.API.ShowMsg(_context.API.GetTranslation("wox_plugin_everything_copied"), "", string.Empty);
                            return false;
                        },
                        IcoPath = "Images\\error.png"
                    });
                }
            }

            return results;
        }

        private IResult? CreateResult(string keyword, SearchResult searchResult, int index)
        {
            var path = searchResult.FullPath;

            string? workingDir = null;
            if (_settings.UseLocationAsWorkingDir)
                workingDir = Path.GetDirectoryName(path);
            if (workingDir == null)
                return null;

            var r = new EverythingResult
            {
                _settings = _settings,
                Score = _settings.MaxSearchCount - index,
                FilePath = searchResult.FullPath,
                workingDir = workingDir,
                Title = searchResult.FileName,
                SubTitle = searchResult.FullPath,
                ContextData = searchResult,
                IconLoader = _context.API.IconHelper.FromAssociatedIcon(searchResult.FullPath)
            };
            return r;
        }


        public Task InitAsync(PluginInitContext context)
        {
            _context = context;
            _storage = new PluginJsonStorage<Settings>();
            _settings = _storage.Load();
            if (_settings.MaxSearchCount <= 0)
            {
                _settings.MaxSearchCount = Settings.DefaultMaxSearchCount;
            }

            var pluginDirectory = context.CurrentPluginMetadata.PluginDirectory;
            const string sdk = "EverythingSDK";
            var sdkDirectory = Path.Combine(pluginDirectory, sdk, CpuType());
            var sdkPath = Path.Combine(sdkDirectory, DLL);
            Logger.WoxDebug($"sdk path <{sdkPath}>");
            Constant.EverythingSDKPath = sdkPath;
            _api.Load(sdkPath);
            return Task.CompletedTask;
        }

        private static string CpuType()
        {
            if (!Environment.Is64BitProcess)
            {
                return "x86";
            }
            else
            {
                return "x64";
            }
        }

        public string GetTranslatedPluginTitle()
        {
            return _context.API.GetTranslation("wox_plugin_everything_plugin_name");
        }

        public string GetTranslatedPluginDescription()
        {
            return _context.API.GetTranslation("wox_plugin_everything_plugin_description");
        }

        public IEnumerable<PluginOption> Options => [
                new CheckBoxOption(){
                    Key = "wox_plugin_everything_use_location_as_working_dir",
                    Value = _settings.UseLocationAsWorkingDir,
                },
                //new CheckBoxOption(){
                //    Key = "",
                //    Value = _settings.MaxSearchCount,
                //}
            ];

        public void SaveOptions(IEnumerable<PluginOption> options)
        {
            _settings.UseLocationAsWorkingDir = options.FirstCheckBoxValue("wox_plugin_everything_use_location_as_working_dir");
            _storage.Save();
        }
    }

    class EverythingResult : IResult
    {
        public string Title { get; init; }

        public string? SubTitle { get; init; }

        public int Score { get; init; }

        public Object ContextData { get; init; }

        internal Settings _settings { get; set; }

        public String FilePath { get; set; }
        public String workingDir { get; set; }

        private List<ContextMenu> GetDefaultContextMenu(ActionContext context)
        {
            List<ContextMenu> defaultContextMenus = new();
            ContextMenu openFolderContextMenu = new()
            {
                Name = context.API.GetTranslation("wox_plugin_everything_open_containing_folder"),
                Command = "explorer.exe",
                Argument = " /select,\"{path}\"",
                ImagePath = "Images\\folder.png"
            };

            defaultContextMenus.Add(openFolderContextMenu);

            string editorPath = string.IsNullOrEmpty(_settings.EditorPath) ? "notepad.exe" : _settings.EditorPath;

            ContextMenu openWithEditorContextMenu = new()
            {
                Name = string.Format(context.API.GetTranslation("wox_plugin_everything_open_with_editor"),System.IO. Path.GetFileNameWithoutExtension(editorPath)),
                Command = editorPath,
                Argument = " \"{path}\"",
                ImagePath = editorPath
            };

            defaultContextMenus.Add(openWithEditorContextMenu);

            return defaultContextMenus;
        }


        public List<IResult> LoadContextMenu(ActionContext context)
        {
            List<IResult> contextMenus = new();
            if (ContextData is not SearchResult record) 
                return contextMenus;

            List<ContextMenu> availableContextMenus = new();
            availableContextMenus.AddRange(GetDefaultContextMenu(context));
            availableContextMenus.AddRange(_settings.ContextMenus);

            if (record.Type == ResultType.File)
            {
                foreach (ContextMenu contextMenu in availableContextMenus)
                {
                    var menu = contextMenu;
                    contextMenus.Add(new Result
                    {
                        Title = contextMenu.Name,
                        Action = _ =>
                        {
                            string argument = menu.Argument.Replace("{path}", record.FullPath);
                            try
                            {
                                Process.Start(menu.Command, argument);
                            }
                            catch
                            {
                                context.API.ShowMsg(string.Format(context.API.GetTranslation("wox_plugin_everything_canot_start"), record.FullPath), string.Empty, string.Empty);
                                return false;
                            }
                            return true;
                        },
                        IcoPath = contextMenu.ImagePath
                    });
                }
            }

            var icoPath = (record.Type == ResultType.File) ? "Images\\file.png" : "Images\\folder.png";
            contextMenus.Add(new Result
            {
                Title = context.API.GetTranslation("wox_plugin_everything_copy_path"),
                AsyncAction = Actions.CopyTextToClipboard(record.FullPath),
                IcoPath = icoPath
            });

            contextMenus.Add(new Result
            {
                Title = context.API.GetTranslation("wox_plugin_everything_copy"),
                AsyncAction = Actions.CopyFilesToClipboard(record.FullPath),
                IcoPath = icoPath
            });

            if (record.Type == ResultType.File || record.Type == ResultType.Folder)
                contextMenus.Add(new Result
                {
                    Title = context.API.GetTranslation("wox_plugin_everything_delete"),
                    Action = (context) =>
                    {
                        try
                        {
                            if (record.Type == ResultType.File)
                                System.IO.File.Delete(record.FullPath);
                            else
                                System.IO.Directory.Delete(record.FullPath);
                        }
                        catch
                        {
                            context.API.ShowMsg(string.Format(context.API.GetTranslation("wox_plugin_everything_canot_delete"), record.FullPath), string.Empty, string.Empty);
                            return false;
                        }

                        return true;
                    },
                    IcoPath = icoPath
                });

            return contextMenus;
        }
        public Task<bool> InvokeAsync(ActionContext context)
        {
            if (File.Exists(FilePath))
                Actions.OpenFile(FilePath, workingDir)(context);
            else
                Actions.OpenDirectory(FilePath)(context);
            return Task.FromResult(true);
        }

        public IconLoader IconLoader { get; set; }
    }
}