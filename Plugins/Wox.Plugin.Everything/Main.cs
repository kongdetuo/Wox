﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using NLog;
using Wox.Infrastructure;
using Wox.Infrastructure.Logger;
using Wox.Infrastructure.Storage;
using Wox.Plugin.Everything.Everything;

namespace Wox.Plugin.Everything
{
    public class Main : IAsyncPlugin, ISettingProvider, IPluginI18n, IContextMenu, ISavable
    {
        public const string DLL = "Everything.dll";
        private readonly EverythingApi _api = new EverythingApi();

        private PluginInitContext _context;

        private Settings _settings;
        private PluginJsonStorage<Settings> _storage;
        private CancellationTokenSource _updateSource;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public void Save()
        {
            _storage.Save();
        }

        public async Task<List<Result>> QueryAsync(Query query, CancellationToken token)
        {
            await Task.Yield();
            var results = new List<Result>();
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
                        results.Add(r);
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
                        Action = _ =>
                        {
                            Clipboard.SetText(e.Message + "\r\n" + e.StackTrace);
                            _context.API.ShowMsg(_context.API.GetTranslation("wox_plugin_everything_copied"), null, string.Empty);
                            return false;
                        },
                        IcoPath = "Images\\error.png"
                    });
                }
            }

            return results;
        }

        private Result CreateResult(string keyword, SearchResult searchResult, int index)
        {
            var path = searchResult.FullPath;

            string workingDir = null;
            if (_settings.UseLocationAsWorkingDir)
                workingDir = Path.GetDirectoryName(path);

            var r = new Result
            {
                Score = _settings.MaxSearchCount - index,
                Title = searchResult.FileName,
                TitleHighlightData = searchResult.FileNameHightData,
                SubTitle = searchResult.FullPath,
                SubTitleHighlightData = searchResult.FullPathHightData,
                IcoPath = searchResult.FullPath,
                Action = File.Exists(path) ? Actions.OpenFile(path, workingDir) : Actions.OpenDirectory(path),
                ContextData = searchResult,
            };
            return r;
        }

        private List<ContextMenu> GetDefaultContextMenu()
        {
            List<ContextMenu> defaultContextMenus = new List<ContextMenu>();
            ContextMenu openFolderContextMenu = new ContextMenu
            {
                Name = _context.API.GetTranslation("wox_plugin_everything_open_containing_folder"),
                Command = "explorer.exe",
                Argument = " /select,\"{path}\"",
                ImagePath = "Images\\folder.png"
            };

            defaultContextMenus.Add(openFolderContextMenu);

            string editorPath = string.IsNullOrEmpty(_settings.EditorPath) ? "notepad.exe" : _settings.EditorPath;

            ContextMenu openWithEditorContextMenu = new ContextMenu
            {
                Name = string.Format(_context.API.GetTranslation("wox_plugin_everything_open_with_editor"), Path.GetFileNameWithoutExtension(editorPath)),
                Command = editorPath,
                Argument = " \"{path}\"",
                ImagePath = editorPath
            };

            defaultContextMenus.Add(openWithEditorContextMenu);

            return defaultContextMenus;
        }

        public async Task InitAsync(PluginInitContext context)
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

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            SearchResult record = selectedResult.ContextData as SearchResult;
            List<Result> contextMenus = new List<Result>();
            if (record == null) return contextMenus;

            List<ContextMenu> availableContextMenus = new List<ContextMenu>();
            availableContextMenus.AddRange(GetDefaultContextMenu());
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
                                _context.API.ShowMsg(string.Format(_context.API.GetTranslation("wox_plugin_everything_canot_start"), record.FullPath), string.Empty, string.Empty);
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
                Title = _context.API.GetTranslation("wox_plugin_everything_copy_path"),
                Action = Actions.CopyTextToClipboard(record.FullPath),
                IcoPath = icoPath
            });

            contextMenus.Add(new Result
            {
                Title = _context.API.GetTranslation("wox_plugin_everything_copy"),
                Action = Actions.CopyFilesToClipboard(record.FullPath),
                IcoPath = icoPath
            });

            if (record.Type == ResultType.File || record.Type == ResultType.Folder)
                contextMenus.Add(new Result
                {
                    Title = _context.API.GetTranslation("wox_plugin_everything_delete"),
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
                            _context.API.ShowMsg(string.Format(_context.API.GetTranslation("wox_plugin_everything_canot_delete"), record.FullPath), string.Empty, string.Empty);
                            return false;
                        }

                        return true;
                    },
                    IcoPath = icoPath
                });

            return contextMenus;
        }

        public Control CreateSettingPanel()
        {
            return new EverythingSettings(_settings);
        }
    }
}