using Avalonia.Media;
using MS.WindowsAPICodePack.Internal;
using NHotkey;
using NHotkey.Wpf;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Wox.Core.Plugin;
using Wox.Core.Resource;
using Wox.Core.Services;
using Wox.Core.Storage;
using Wox.Helper;
using Wox.Infrastructure;
using Wox.Infrastructure.Hotkey;
using Wox.Infrastructure.Storage;
using Wox.Infrastructure.UserSettings;
using Wox.Plugin;

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
            this.QueryService = new QueryService(_topMostRecord, _userSelectedRecord, App.API, _history);




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

        public ResultsViewModel Results { get; private set => this.RaiseAndSetIfChanged(ref field, value); }
        public ResultsViewModel ContextMenu { get; private set => this.RaiseAndSetIfChanged(ref field, value); }
        public ResultsViewModel History { get; private set => this.RaiseAndSetIfChanged(ref field, value); }

        public string? PluginID { get; set; }

        WoxPlugin PluginProxy { get; set; }

        public ObservableCollection<object> Icons { get; set; } = new ObservableCollection<object>();

        public object SelectedPlugin { get; set => this.RaiseAndSetIfChanged(ref field, value); }

        public string QueryText { get; set => this.RaiseAndSetIfChanged(ref field, value); } = string.Empty;

        /// <summary>
        /// we need move cursor to end when we manually changed query
        /// but we don't want to move cursor to end when query is updated from TextBox
        /// </summary>
        /// <param name="queryText"></param>
        public void ChangeQueryText(string queryText)
        {
            if (string.IsNullOrEmpty(queryText))
                queryText = string.Empty;
            if (SelectedResults != Results)
            {
                _queryTextBeforeLeaveResults = queryText;
                SelectedResults = Results; // 重置为查询状态
            }
            QueryText = queryText;
            CaretIndex = queryText.Length;

        }

        public bool LastQuerySelected { get; set; }
        public int CaretIndex { get; set => this.RaiseAndSetIfChanged(ref field, value); }

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

        public bool ShowProcessBar { get; set => this.RaiseAndSetIfChanged(ref field, value); }
        public bool ShowMainWindow { get; set=> this.RaiseAndSetIfChanged(ref field, value); }


        #endregion ViewModel Properties

        #region Commands

        public ICommand EscCommand => field ??= new RelayCommand(_ =>
        {
            if (!SelectedIsFromQueryResults)
            {
                SelectedResults = Results;
                UpdateResultVisible();
            }
            else
            {
                ShowMainWindow = false;
            }
        });

        public ICommand StartHelpCommand => field ??= new RelayCommand(_ =>
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = "http://doc.wox.one/",
                UseShellExecute = true
            });
        });
        public ICommand RefreshCommand => field ??= new RelayCommand(_ => Refresh());
        public ICommand LoadContextMenuCommand => field ??= new RelayCommand(_ =>
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
        public ICommand LoadHistoryCommand => field ?? new RelayCommand(_ =>
        {
            if (SelectedIsFromQueryResults)
            {
                SelectedResults = History;
                History.SelectedIndex = 0;
            }
            else
            {
                SelectedResults = Results;
            }
        });
        public ICommand OpenResultCommand => field ??= ReactiveCommand.CreateFromTask<ResultViewModel>(async resultVM =>
        {
            if (resultVM != null)
            {
                try
                {
                bool queryResult = SelectedIsFromQueryResults;

                bool hideWindow = false;
                var result = resultVM.Result;

                hideWindow = await result.Result.InvokeAsync(new ActionContext
                {
                    SpecialKeyState = GlobalHotkey.Instance.CheckModifiers(),
                    API = App.API
                });

                if (hideWindow)
                {
                    ShowMainWindow = false;
                }

                if (queryResult)
                {
                    _userSelectedRecord.Add(result);
                    _history.Add(resultVM.Query!.RawQuery);
                }
                else
                {
                    SelectedResults = Results;
                }
                }
                catch (Exception)
                {

    
                }

            }
        });

        public ICommand AutoComplationCommand => field ??= new RelayCommand(_ =>
        {
            var result = SelectedResults.Results.FirstOrDefault()?.Result;
            if (result is not null && result.Title.StartsWith(QueryText, true, null))
            {
                ChangeQueryText(result.Title);
            }
        });

        public readonly Interaction<Unit, Unit> ShowSettingInteraction = new Interaction<Unit, Unit>();

        public ICommand OpenSettingCommand => field ??= ReactiveCommand.Create(() =>
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
                .Throttle(TimeSpan.FromMilliseconds(100))
                .DistinctUntilChanged();

            var querys = queryTextChangeds
                .Where(_ => SelectedIsFromQueryResults)
                .Select(p => p.TrimStart())
                .DistinctUntilChanged()
                .Select(QueryBuilder.Build);

            // 每一个插件单独查询返回一个 IObservable<T> 然后合并成最新结果
            // 每60毫秒取一次最新结果显示出来
            PluginManager.ActivePlugins
                .Select(plugin => querys
                    .Select(query => QueryService.Query(plugin, query))
                    .Switch())
                .CombineLatest()
                .Sample(TimeSpan.FromMilliseconds(60))
                .ObserveOn(SynchronizationContext.Current!)
                .Subscribe(this.Results.SetResult);

            this.WhenAnyValue(p => p.SelectedResults)
                .Where(p => p == ContextMenu)
                .Select(p => Results.SelectedItem)
                .Where(p => p != null)
                .Select(p => QueryService.QueryContextMenuAsync(p.Result, p.Query!))
                .Switch()
                .Subscribe(ContextMenu.SetResult);

            this.WhenAnyValue(p => p.SelectedResults)
                .Where(p => p == History)
                .Select(p => queryTextChangeds.StartWith(""))
                .Switch()
                .Select(QueryService.QueryHistory)
                .Subscribe(History.SetResult);
        }

        private void Results_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            SetPluginIcon();
        }

        private static void Refresh()
        {
            PluginManager.ReloadData();
        }


        #endregion

        private bool SelectedIsFromQueryResults => SelectedResults == Results;

        private bool ContextMenuSelected => SelectedResults == ContextMenu;

        private bool HistorySelected => SelectedResults == History;

        private void SetPluginIcon()
        {
            var queryText = QueryText.TrimStart();
            if (SelectedIsFromQueryResults)
            {
                if (string.IsNullOrEmpty(queryText))
                {
                    this.PluginIcon = Image.ImageLoader.GetErrorImage();
                }
                var icons = this.SelectedResults.Results
                    .Select(p => p.Result.Plugin)
                    .Distinct()
                    .Take(3)
                    .ToList();
                if (icons.Any())
                {
                    Icons.Clear();
                    for (int i = 0; i < icons.Count; i++)
                    {
                        Icons.Add(icons[i]);
                    }
                }
                else
                {
                    Icons.Clear();
                    //Icons.Add(Image.ImageLoader.GetErrorImage());
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

        private void UpdateResultVisible()
        {
            if (ContextMenu != SelectedResults)
                ContextMenu.IsVisible = false;
            if (Results != SelectedResults)
                Results.IsVisible = false;
            if (History != SelectedResults)
                History.IsVisible = false;

            SelectedResults.IsVisible = SelectedResults.Count > 0;

            SetPluginIcon();
        }

        #endregion Public Methods
    }

    public class ViewModelBase : ReactiveObject
    {
    }
}