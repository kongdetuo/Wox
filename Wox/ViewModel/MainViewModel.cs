using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using NHotkey;
using NHotkey.Wpf;

using Wox.Core.Plugin;
using Wox.Core.Resource;
using Wox.Core.Services;
using Wox.Helper;
using Wox.Infrastructure;
using Wox.Infrastructure.Hotkey;
using Wox.Infrastructure.Storage;
using Wox.Infrastructure.UserSettings;
using Wox.Plugin;
using Wox.Core.Storage;
using System.Reactive.Concurrency;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Avalonia.Media;

namespace Wox.ViewModel
{
    public class MainViewModel : ViewModelBase, ISavable
    {
        #region Private Fields

        private string _queryTextBeforeLeaveResults = string.Empty;

        private readonly WoxJsonStorage<History> _historyItemsStorage;
        private readonly WoxJsonStorage<UserSelectedRecord> _userSelectedRecordStorage;
        private readonly WoxJsonStorage<TopMostRecord> _topMostRecordStorage;
        private readonly Settings _settings;
        private readonly History _history;
        private readonly UserSelectedRecord _userSelectedRecord;
        private readonly TopMostRecord _topMostRecord;

        private bool _saved;

        private readonly Internationalization? _translator;


        private readonly SynchronizationContext SynchronizationContext;
        private readonly QueryService QueryService;

        #endregion Private Fields

        #region Constructor

        public MainViewModel()
        {
            _settings = Settings.Instance;

            _historyItemsStorage = new WoxJsonStorage<History>();
            _userSelectedRecordStorage = new WoxJsonStorage<UserSelectedRecord>();
            _topMostRecordStorage = new WoxJsonStorage<TopMostRecord>();
            _history = _historyItemsStorage.Load();
            _userSelectedRecord = _userSelectedRecordStorage.Load();
            _topMostRecord = _topMostRecordStorage.Load();
            this.QueryService = new QueryService(_topMostRecord, _userSelectedRecord);


            ContextMenu = new ResultsViewModel(_settings, UpdateResultVisible);
            Results = new ResultsViewModel(_settings, UpdateResultVisible);
            History = new ResultsViewModel(_settings, UpdateResultVisible);
            _selectedResults = Results;

            this.SynchronizationContext = SynchronizationContext.Current!;

            _translator = InternationalizationManager.Instance;

            SetHotkey();

            InitQuery();
        }
        #endregion Constructor

        #region ViewModel Properties

        [Reactive] public ResultsViewModel Results { get; private set; }
        [Reactive] public ResultsViewModel ContextMenu { get; private set; }
        [Reactive] public ResultsViewModel History { get; private set; }

        public string? PluginID { get; set; }

        [Reactive] public string QueryText { get; set; } = string.Empty;

        /// <summary>
        /// we need move cursor to end when we manually changed query
        /// but we don't want to move cursor to end when query is updated from TextBox
        /// </summary>
        /// <param name="queryText"></param>
        public void ChangeQueryText(string queryText)
        {
            if(string.IsNullOrEmpty(queryText))
                queryText = string.Empty;
            QueryText = queryText;
            CaretIndex = queryText.Length;
        }

        public bool LastQuerySelected { get; set; }
        [Reactive] public int CaretIndex { get; set; }

        public bool ShowIcon => PluginIcon is null;

        public IImage? PluginIcon { get; set; }


        private ResultsViewModel _selectedResults;


        public ResultsViewModel SelectedResults
        {
            get { return _selectedResults; }
            set
            {
                _selectedResults = value;
                if (SelectedIsFromQueryResults)
                {
                    // use DistinctUntilChanged operator to avoid duplicate query
                    ChangeQueryText(_queryTextBeforeLeaveResults);
                    UpdateResultVisible();
                }
                else
                {
                    _queryTextBeforeLeaveResults = QueryText;
                    if (string.IsNullOrEmpty(QueryText))
                        this.RaisePropertyChanged(nameof(QueryText));
                    QueryText = string.Empty;
                }
                this.RaisePropertyChanged();
            }
        }

        [Reactive] public bool ShowProcessBar { get; set; }
        [Reactive] public bool ShowMainWindow { get; set; }


        #endregion ViewModel Properties

        #region Commands
        private ICommand? loadContextMenuCommand;
        private ICommand? loadHistoryCommand;
        private ICommand? openResultCommand;
        private ICommand? autoComplationCommand;
        private ICommand? startHelpCommand;
        private ICommand? refreshCommand;
        private ICommand? escCommand;
        private ReactiveCommand<Unit, Unit> openSettingCommand;

        public ICommand EscCommand => escCommand ??= new RelayCommand(_ =>
        {
            if (!SelectedIsFromQueryResults)
            {
                SelectedResults = Results;
            }
            else
            {
                ShowMainWindow = false;
            }
        });

        public ICommand StartHelpCommand => startHelpCommand ??= new RelayCommand(_ =>
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = "http://doc.wox.one/",
                UseShellExecute = true
            });
        });
        public ICommand RefreshCommand => refreshCommand ??= new RelayCommand(_ => Refresh());
        public ICommand LoadContextMenuCommand => loadContextMenuCommand ??= new RelayCommand(_ =>
        {
            if (SelectedIsFromQueryResults)
            {
                SelectedResults = ContextMenu;
            }
            else
            {
                SelectedResults = Results;
            }
        });
        public ICommand LoadHistoryCommand => loadHistoryCommand ?? new RelayCommand(_ =>
        {
            if (SelectedIsFromQueryResults)
            {
                SelectedResults = History;
                History.SelectedIndex = _history.Items.Count - 1;
            }
            else
            {
                SelectedResults = Results;
            }
        });
        public ICommand OpenResultCommand => openResultCommand ??= new RelayCommand(obj =>
        {
            Task.Run(async () =>
            {
                var resultVM = obj switch
                {
                    ResultViewModel r => r,
                    string ss when int.TryParse(ss, out var index) && index < SelectedResults.Count => SelectedResults.Results[index],
                    _ => null
                };

                if (resultVM != null)
                {
                    var context = new ActionContext
                    {
                        SpecialKeyState = GlobalHotkey.Instance.CheckModifiers(),
                        API = App.API
                    };

                    bool hideWindow = false;
                    var result = resultVM.Result;
                    if (result.AsyncAction is not null)
                        hideWindow = await result.AsyncAction(context);
                    else if (result.Action is not null)
                        hideWindow = result.Action(context);

                    if (hideWindow)
                    {
                        ShowMainWindow = false;
                    }

                    if (SelectedIsFromQueryResults)
                    {
                        _userSelectedRecord.Add(result);
                        _history.Add(resultVM.Query!.RawQuery);
                    }
                    else
                    {
                        SelectedResults = Results;
                    }
                }
            });
        });

        public ICommand AutoComplationCommand => autoComplationCommand ??= new RelayCommand(_ =>
        {
            var result = SelectedResults.Results.FirstOrDefault()?.Result;
            if (result is not null && result.Title.Text.StartsWith(QueryText, true, null))
            {
                ChangeQueryText(result.Title.Text);
            }
        });

        public readonly Interaction<Unit, Unit> ShowSettingInteraction = new Interaction<Unit, Unit>();

        public ICommand OpenSettingCommand => openSettingCommand ??= ReactiveCommand.Create(() =>
        {
           // new 
        });

        public async void OpenSetting()
        {
            await ShowSettingInteraction.Handle(Unit.Default);
        }

        #endregion

        #region Query
        private void InitQuery()
        {
            var queryTextChangeds = this.WhenAnyValue(p => p.QueryText)
                //.Where(p => MainWindowVisibility == Visibility.Visible)
                .Publish();

            var querys = queryTextChangeds.Where(_ => SelectedIsFromQueryResults)
                .Select(p => p.TrimStart())
                .DistinctUntilChanged()
                .ObserveOn(ThreadPoolScheduler.Instance)
                .Throttle(TimeSpan.FromMilliseconds(20))
                .Select(p => QueryService.Query(QueryBuilder.Build(p)))
                .Publish();

            querys
                .Switch()
                .Buffer(TimeSpan.FromMilliseconds(60))
                .Where(p => p.Count > 0)
                .ObserveOn(this.SynchronizationContext)
                .Subscribe(p =>
                {
                    UpdateResultView(p, CancellationToken.None);
                });

            querys.Select(CreateProgressBarVisibles)
                .Switch().DistinctUntilChanged()
                .ObserveOn(this.SynchronizationContext)
                .Subscribe(p => ShowProcessBar = p);

            querys.Connect();


            _ = queryTextChangeds.Where(p => ContextMenuSelected)
                .Select(queryText => Observable.FromAsync(token => QueryContextMenuAsync(token)))
                .Switch().Subscribe();

            _ = queryTextChangeds.Where(p => HistorySelected)
                .Subscribe(queryText => QueryHistory());


            queryTextChangeds.Connect();
        }

        private static IObservable<bool> CreateProgressBarVisibles(IObservable<PluginQueryResult> queryViewModel)
        {
            return Observable.Create<bool>(async (ob, token) =>
            {
                try
                {
                    await Task.Delay(200, token);
                    if (token.IsCancellationRequested)
                        return;
                    ob.OnNext(true);

                    await queryViewModel;
                    if (token.IsCancellationRequested)
                        return;
                    ob.OnNext(false);
                }
                catch (Exception)
                {
                }
                finally
                {
                    ob.OnCompleted();
                }
            });
        }
        private async Task<Unit> QueryContextMenuAsync(CancellationToken token)
        {
            const string id = "Context Menu ID";
            var query = QueryText.ToLower().Trim();
            ContextMenu.Clear();

            var selected = Results.SelectedItem;

            if (selected != null) // SelectedItem returns null if selection is empty.
            {
                var results = await PluginManager.GetContextMenusForPlugin(selected.PluginMetadata!.ID, selected.Result);
                results.Add(ContextMenuTopMost(selected));
                results.Add(ContextMenuPluginInfo(selected.PluginMetadata.ID));

                if (!string.IsNullOrEmpty(query))
                    results = results.Where(r => MatchResult(r, query)).ToList();
                if (token.IsCancellationRequested == false)
                {
                    ContextMenu.AddResults(results, id);
                    UpdateResultVisible();
                }
            }
            return Unit.Default;
        }

        private void QueryHistory()
        {
            const string id = "Query History ID";
            var query = QueryText.ToLower().Trim();
            History.Clear();

            var results = new List<Result>();
            foreach (var h in _history.Items)
            {
                var title = _translator!.GetTranslation("executeQuery");
                var time = _translator.GetTranslation("lastExecuteTime");
                var result = new Result
                {
                    Title = string.Format(title, h.Query),
                    SubTitle = string.Format(time, h.ExecutedDateTime),
                    IcoPath = "Images\\history.png",
                    Action = _ =>
                    {
                        SelectedResults = Results;
                        ChangeQueryText(h.Query);
                        return false;
                    }
                };
                results.Add(result);
            }

            if (!string.IsNullOrEmpty(query))
                results = results.Where(r => MatchResult(r, query)).ToList();

            History.AddResults(results, id);
            UpdateResultVisible();
        }

        private static bool MatchResult(Result result, string query)
        {
            return StringMatcher.FuzzySearch(query, result.Title.Text).IsSearchPrecisionScoreMet()
                || StringMatcher.FuzzySearch(query, result.SubTitle.Text).IsSearchPrecisionScoreMet();
        }

        private static void Refresh()
        {
            PluginManager.ReloadData();
        }

        private Result ContextMenuTopMost(ResultViewModel result)
        {
            Result menu;
            if (_topMostRecord.IsTopMost(result.Query!, result.PluginMetadata!.ID, result.Result))
            {
                menu = new Result
                {
                    Title = InternationalizationManager.Instance.GetTranslation("cancelTopMostInThisQuery"),
                    IcoPath = "Images\\down.png",
                    Action = _ =>
                    {
                        _topMostRecord.Remove(result.Query!);
                        App.API.ShowMsg("Success");
                        return false;
                    }
                };
            }
            else
            {
                menu = new Result
                {
                    Title = InternationalizationManager.Instance.GetTranslation("setAsTopMostInThisQuery"),
                    IcoPath = "Images\\up.png",
                    Action = _ =>
                    {
                        _topMostRecord.AddOrUpdate(result.Query!, result.PluginMetadata.ID, result.Result);
                        App.API.ShowMsg("Success");
                        return false;
                    }
                };
            }
            return menu;
        }

        private static Result ContextMenuPluginInfo(string id)
        {
            var metadata = PluginManager.GetPluginForId(id)!.Metadata;
            var translator = InternationalizationManager.Instance;

            var author = translator.GetTranslation("author");
            var website = translator.GetTranslation("website");
            var version = translator.GetTranslation("version");
            var plugin = translator.GetTranslation("plugin");
            var title = $"{plugin}: {metadata.Name}";
            var icon = metadata.IcoPath;
            var subtitle = $"{author}: {metadata.Author}, {website}: {metadata.Website} {version}: {metadata.Version}";

            var menu = new Result
            {
                Title = title,
                IcoPath = icon,
                SubTitle = subtitle,
                Action = _ => false
            };
            return menu;
        }



        #endregion

        private bool SelectedIsFromQueryResults => SelectedResults == Results;

        private bool ContextMenuSelected => SelectedResults == ContextMenu;

        private bool HistorySelected => SelectedResults == History;

        private void SetPluginIcon()
        {
            var queryText = QueryText.AsSpan().TrimStart();
            if (SelectedIsFromQueryResults)
            {
                if (this.Results.PluginID != this.PluginID)
                {
                    this.PluginID = this.Results.PluginID;
                    var plugin = PluginManager.GetPluginForId(PluginID);
                    if (plugin == null && queryText.Contains(' '))
                    {
                        var key = queryText[..queryText.IndexOf(' ')].ToString();
                        var plugins = PluginManager.AllPlugins
                            .Where(p => !p.Metadata.Disabled && p.MatchKeyWord(key))
                            .Take(2).ToArray();
                        if (plugins.Length == 1)
                            plugin = plugins[0];
                    }

                    if (plugin != null)
                        this.PluginIcon = Image.ImageLoader.Load(plugin.Metadata.IcoPath, plugin.Metadata.PluginDirectory);
                    else
                        this.PluginIcon = null;
                }
            }
            else if (ContextMenuSelected)
            {
                this.PluginIcon = Image.ImageLoader.GetErrorImage();
            }
            else if (HistorySelected)
            {
                this.PluginIcon = Image.ImageLoader.Load("Images/history.png", "");
            }
            else
            {
                this.PluginIcon = null;
            }
        }

        #region Hotkey

        private void SetHotkey()
        {
            SetHotkey(_settings.Hotkey, OnWoxHotkey);
            SetCustomPluginHotkey();
        }

        private void SetCustomPluginHotkey()
        {
            if (_settings.CustomPluginHotkeys == null)
                return;
            foreach (CustomPluginHotkey hotkey in _settings.CustomPluginHotkeys)
            {
                SetHotkey(hotkey.Hotkey, (s, e) =>
                {
                    if (ShouldIgnoreHotkeys()) return;
                    ShowMainWindow = true;
                    ChangeQueryText(hotkey.ActionKeyword);
                });
            }
        }

        private static void SetHotkey(string hotkeyStr, EventHandler<HotkeyEventArgs> action)
        {
            try
            {
                var hotkey = new HotkeyModel(hotkeyStr);
                HotkeyManager.Current.AddOrReplace(hotkeyStr, hotkey.CharKey, hotkey.ModifierKeys, action);
            }
            catch (Exception)
            {
                string errorMsg =
                    string.Format(InternationalizationManager.Instance.GetTranslation("registerHotkeyFailed"), hotkeyStr);
                MessageBox.Show(errorMsg);
            }
        }

        public static void RemoveHotkey(string hotkeyStr)
        {
            if (!string.IsNullOrEmpty(hotkeyStr))
            {
                HotkeyManager.Current.Remove(hotkeyStr);
            }
        }

        /// <summary>
        /// Checks if Wox should ignore any hotkeys
        /// </summary>
        /// <returns></returns>
        private bool ShouldIgnoreHotkeys()
        {
            //double if to omit calling win32 function
            return _settings.IgnoreHotkeysOnFullscreen && WindowsInteropHelper.IsWindowFullscreen();
        }


        private void OnWoxHotkey(object? sender, HotkeyEventArgs e)
        {
            if (!ShouldIgnoreHotkeys())
            {
                if (_settings.LastQueryMode == LastQueryMode.Empty)
                {
                    ChangeQueryText(string.Empty);
                }
                else if (_settings.LastQueryMode == LastQueryMode.Preserved)
                {
                    LastQuerySelected = true;
                }
                else if (_settings.LastQueryMode == LastQueryMode.Selected)
                {
                    LastQuerySelected = false;
                }
                else
                {
                    throw new ArgumentException($"wrong LastQueryMode: <{_settings.LastQueryMode}>");
                }

                ToggleWox();
                e.Handled = true;
            }
        }

        private void ToggleWox()
        {
            ShowMainWindow = !ShowMainWindow;
        }

        #endregion Hotkey

        #region Public Methods

        public void Save()
        {
            if (!_saved)
            {
                _historyItemsStorage.Save();
                _userSelectedRecordStorage.Save();
                _topMostRecordStorage.Save();

                _saved = true;
            }
        }

        /// <summary>
        /// To avoid deadlock, this method should not called from main thread
        /// </summary>
        public void UpdateResultView(IList<PluginQueryResult> updates, CancellationToken token)
        {
            Results.AddResults(updates, token);
            UpdateResultVisible();
            SetPluginIcon();
        }

        private void UpdateResultVisible()
        {
            if (ContextMenu != SelectedResults)
                ContextMenu.IsVisible = false;
            if (Results != SelectedResults)
                Results.IsVisible = false;
            if (History != SelectedResults)
                History.IsVisible = false;

            SelectedResults.IsVisible = SelectedResults.Count > 0;
        }

        #endregion Public Methods
    }

    public class ViewModelBase : ReactiveObject
    {
    }
}