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
using System.Windows.Media;
using NHotkey;
using NHotkey.Wpf;
using NLog;

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
using System.Threading.Channels;
using System.ComponentModel;
using System.Reactive.Concurrency;

namespace Wox.ViewModel
{
    public class MainViewModel : BaseModel, ISavable
    {
        #region Private Fields

        private string _queryTextBeforeLeaveResults;

        private readonly WoxJsonStorage<History> _historyItemsStorage;
        private readonly WoxJsonStorage<UserSelectedRecord> _userSelectedRecordStorage;
        private readonly WoxJsonStorage<TopMostRecord> _topMostRecordStorage;
        private readonly Settings _settings;
        private readonly History _history;
        private readonly UserSelectedRecord _userSelectedRecord;
        private readonly TopMostRecord _topMostRecord;

        private bool _saved;

        private readonly Internationalization _translator;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly SynchronizationContext SynchronizationContext;
        private readonly QueryService QueryService;


        #endregion Private Fields

        #region Constructor

        public MainViewModel(bool useUI = true)
        {
            _saved = false;
            _queryTextBeforeLeaveResults = "";
            QueryText = "";

            _settings = Settings.Instance;

            _historyItemsStorage = new WoxJsonStorage<History>();
            _userSelectedRecordStorage = new WoxJsonStorage<UserSelectedRecord>();
            _topMostRecordStorage = new WoxJsonStorage<TopMostRecord>();
            _history = _historyItemsStorage.Load();
            _userSelectedRecord = _userSelectedRecordStorage.Load();
            _topMostRecord = _topMostRecordStorage.Load();
            this.QueryService = new QueryService(_topMostRecord, _userSelectedRecord);


            ContextMenu = new ContextViewModel(_settings);
            Results = new QueryResultViewModel(_settings, _userSelectedRecord, _history);
            History = new HistoryViewModel(_settings);
            _selectedResults = Results;

            this.SynchronizationContext = SynchronizationContext.Current;
            if (useUI)
            {
                _translator = InternationalizationManager.Instance;
                InitializeKeyCommands();
                SetHotkey(_settings.Hotkey, OnHotkey);
                SetCustomPluginHotkey();
            }

            var queryTextChangeds = this.WhenChanged(p => p.QueryText).Skip(1)
                .Where(p => MainWindowVisibility == Visibility.Visible)
                .Publish();

            var querys = queryTextChangeds.Where(_ => SelectedIsFromQueryResults())
                .Select(p => p.TrimStart())
                .DistinctUntilChanged()
                .ObserveOn(ThreadPoolScheduler.Instance)
                .Throttle(TimeSpan.FromMilliseconds(20))
                .Publish();


            querys
                .Select(p => QueryService.Query(QueryBuilder.Build(p)))
                .Switch()
                .Buffer(TimeSpan.FromMilliseconds(20))
                .Where(p => p.Count > 0)
                .ObserveOn(this.SynchronizationContext)
                .Subscribe(p =>
                {
                    UpdateResultView(p, CancellationToken.None);
                });

            querys.Connect();


            _ = queryTextChangeds.Where(p => ContextMenuSelected())
                .Select(queryText => Observable.FromAsync(token => QueryContextMenuAsync(token)))
                .Switch().Subscribe();

            _ = queryTextChangeds.Where(p => HistorySelected())
                .Subscribe(queryText => QueryHistory());


            queryTextChangeds.Connect();
        }

        private void SetProcessBar(IObservable<QueryViewModel> observable)
        {
            IObservable<bool> observable1(QueryViewModel vm)
            {
                var start = vm.WhenChanged(p => p.Started).Delay(TimeSpan.FromMilliseconds(200)).Take(1).Select(p => true);
                var end = vm.WhenChanged(p => p.IsCompleted).Where(p => p).Select(p => false);
                return start.Merge(end).TakeUntil(p => p == false);
            }

            _ = observable.SelectMany(p => observable1(p)).DistinctUntilChanged()
                .Subscribe(p => this.ProgressBarVisibility = p ? Visibility.Visible : Visibility.Hidden);
        }


        private void InitializeKeyCommands()
        {
            EscCommand = new RelayCommand(_ =>
            {
                if (!SelectedIsFromQueryResults())
                {
                    SelectedResults = Results;
                }
                else
                {
                    MainWindowVisibility = Visibility.Collapsed;
                }
            });

            StartHelpCommand = new RelayCommand(_ => Process.Start(new ProcessStartInfo() { FileName = "http://doc.wox.one/", UseShellExecute = true }));

            RefreshCommand = new RelayCommand(_ => Refresh());

            OpenResultCommand = new RelayCommand(obj =>
            {
                Task.Run(async () =>
                {
                    var result = (obj as ResultViewModel).Result;
                    if (result != null)
                    {
                        var context = new ActionContext
                        {
                            SpecialKeyState = GlobalHotkey.Instance.CheckModifiers(),
                            API = App.API
                        };

                        bool hideWindow = false;
                        if (result.AsyncAction is not null)
                            hideWindow = await result.AsyncAction(context);
                        else if(result.Action is not null)
                            hideWindow = result.Action(context);

                        if (hideWindow)
                        {
                            MainWindowVisibility = Visibility.Collapsed;
                        }

                        if (SelectedIsFromQueryResults())
                        {
                            _userSelectedRecord.Add(result);
                            _history.Add(result.OriginQuery.RawQuery);
                        }
                        else
                        {
                            SelectedResults = Results;
                        }
                    }
                });
            });

            LoadContextMenuCommand = new RelayCommand(_ =>
            {
                if (SelectedIsFromQueryResults())
                {
                    SelectedResults = ContextMenu;
                }
                else
                {
                    SelectedResults = Results;
                }
            });

            LoadHistoryCommand = new RelayCommand(_ =>
            {
                if (SelectedIsFromQueryResults())
                {
                    SelectedResults = History;
                    History.SelectedIndex = _history.Items.Count - 1;
                }
                else
                {
                    SelectedResults = Results;
                }
            });

            this.AutoComplationCommand = new RelayCommand(_ =>
            {
                var result = SelectedResults.Results.FirstOrDefault()?.Result;
                if (result is not null && result.Title.Text.StartsWith(QueryText, true, null))
                {
                    ChangeQueryText(result.Title.Text);
                }
            });
        }

        #endregion Constructor

        #region ViewModel Properties

        public ResultsViewModel Results { get; private set; }
        public ResultsViewModel ContextMenu { get; private set; }
        public ResultsViewModel History { get; private set; }

        public string PluginID { get; set; }

        public string QueryText { get; set; }

        /// <summary>
        /// we need move cursor to end when we manually changed query
        /// but we don't want to move cursor to end when query is updated from TextBox
        /// </summary>
        /// <param name="queryText"></param>
        public void ChangeQueryText(string queryText)
        {
            QueryTextCursorMovedToEnd = true;
            QueryText = queryText;
        }

        public bool LastQuerySelected { get; set; }
        public bool QueryTextCursorMovedToEnd { get; set; }

        public bool ShowIcon => PluginIcon is null;

        public ImageSource PluginIcon { get; set; }


        private ResultsViewModel _selectedResults;

        public ResultsViewModel SelectedResults
        {
            get { return _selectedResults; }
            set
            {
                _selectedResults = value;
                if (SelectedIsFromQueryResults())
                {
                    // use DistinctUntilChanged operator to avoid duplicate query
                    ChangeQueryText(_queryTextBeforeLeaveResults);
                    UpdateResultVisible();
                }
                else
                {
                    _queryTextBeforeLeaveResults = QueryText;
                    if (string.IsNullOrEmpty(QueryText))
                        OnPropertyChanged(nameof(QueryText));
                    QueryText = string.Empty;
                }
                OnPropertyChanged();
            }
        }

        public Visibility ProgressBarVisibility { get; set; }
        public Visibility MainWindowVisibility { get; set; }
        public ICommand EscCommand { get; set; }

        public ICommand StartHelpCommand { get; set; }
        public ICommand RefreshCommand { get; set; }
        public ICommand LoadContextMenuCommand { get; set; }
        public ICommand LoadHistoryCommand { get; set; }
        public ICommand OpenResultCommand { get; set; }

        public ICommand AutoComplationCommand { get; set; }

        #endregion ViewModel Properties

        private async Task<Unit> QueryContextMenuAsync(CancellationToken token)
        {
            const string id = "Context Menu ID";
            var query = QueryText.ToLower().Trim();
            ContextMenu.Clear();

            var selected = Results.SelectedItem?.Result;

            if (selected != null) // SelectedItem returns null if selection is empty.
            {
                var results = await PluginManager.GetContextMenusForPlugin(selected);
                results.Add(ContextMenuTopMost(selected));
                results.Add(ContextMenuPluginInfo(selected.PluginID));

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
                var title = _translator.GetTranslation("executeQuery");
                var time = _translator.GetTranslation("lastExecuteTime");
                var result = new Result
                {
                    Title = string.Format(title, h.Query),
                    SubTitle = string.Format(time, h.ExecutedDateTime),
                    IcoPath = "Images\\history.png",
                    OriginQuery = new Query { RawQuery = h.Query },
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

        private void Refresh()
        {
            PluginManager.ReloadData();
        }

        private Result ContextMenuTopMost(Result result)
        {
            Result menu;
            if (_topMostRecord.IsTopMost(result))
            {
                menu = new Result
                {
                    Title = InternationalizationManager.Instance.GetTranslation("cancelTopMostInThisQuery"),
                    IcoPath = "Images\\down.png",
                    Action = _ =>
                    {
                        _topMostRecord.Remove(result);
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
                        _topMostRecord.AddOrUpdate(result);
                        App.API.ShowMsg("Success");
                        return false;
                    }
                };
            }
            return menu;
        }

        private Result ContextMenuPluginInfo(string id)
        {
            var metadata = PluginManager.GetPluginForId(id).Metadata;
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

        private bool SelectedIsFromQueryResults()
        {
            var selected = SelectedResults is QueryResultViewModel;
            return selected;
        }

        private bool ContextMenuSelected()
        {
            var selected = SelectedResults == ContextMenu;
            return selected;
        }

        private bool HistorySelected()
        {
            var selected = SelectedResults == History;
            return selected;
        }

        private void SetPluginIcon()
        {
            var queryText = QueryText.AsSpan().TrimStart();
            if (SelectedIsFromQueryResults())
            {
                if (this.Results.PluginID != this.PluginID)
                {
                    this.PluginID = this.Results.PluginID;
                    var plugin = PluginManager.GetPluginForId(this.Results.PluginID);
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
            else if (ContextMenuSelected())
            {
                this.PluginIcon = Image.ImageLoader.GetErrorImage();
            }
            else if (HistorySelected())
            {
                this.PluginIcon = Image.ImageLoader.Load("Images/history.png", "");
            }
            else
            {
                this.PluginIcon = null;
            }
        }

        #region Hotkey

        private void SetHotkey(string hotkeyStr, EventHandler<HotkeyEventArgs> action)
        {
            var hotkey = new HotkeyModel(hotkeyStr);
            SetHotkey(hotkey, action);
        }

        private void SetHotkey(HotkeyModel hotkey, EventHandler<HotkeyEventArgs> action)
        {
            string hotkeyStr = hotkey.ToString();
            try
            {
                HotkeyManager.Current.AddOrReplace(hotkeyStr, hotkey.CharKey, hotkey.ModifierKeys, action);
            }
            catch (Exception)
            {
                string errorMsg =
                    string.Format(InternationalizationManager.Instance.GetTranslation("registerHotkeyFailed"), hotkeyStr);
                MessageBox.Show(errorMsg);
            }
        }

        public void RemoveHotkey(string hotkeyStr)
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

        private void SetCustomPluginHotkey()
        {
            if (_settings.CustomPluginHotkeys == null) return;
            foreach (CustomPluginHotkey hotkey in _settings.CustomPluginHotkeys)
            {
                SetHotkey(hotkey.Hotkey, (s, e) =>
                {
                    if (ShouldIgnoreHotkeys()) return;
                    MainWindowVisibility = Visibility.Visible;
                    ChangeQueryText(hotkey.ActionKeyword);
                });
            }
        }

        private void OnHotkey(object sender, HotkeyEventArgs e)
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
            if (MainWindowVisibility != Visibility.Visible)
            {
                MainWindowVisibility = Visibility.Visible;
            }
            else
            {
                MainWindowVisibility = Visibility.Collapsed;
            }
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
                ContextMenu.Visbility = Visibility.Collapsed;
            if (Results != SelectedResults)
                Results.Visbility = Visibility.Collapsed;
            if (History != SelectedResults)
                History.Visbility = Visibility.Collapsed;

            SelectedResults.Visbility = SelectedResults.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion Public Methods
    }

    public class QueryViewModel : BaseModel
    {
        public QueryViewModel(string queryText)
        {
            this.QueryText = queryText;
        }

        public string QueryText { get; set; }

        public QueryResultViewModel Results { get; set; }

        public bool Started { get; set; }
        public bool IsCompleted { get; set; }
        public bool Canceled { get; set; }
    }
}