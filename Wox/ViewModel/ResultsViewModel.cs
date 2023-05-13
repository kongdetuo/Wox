using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using NLog;
using Wox.Core.Services;
using Wox.Infrastructure.Hotkey;
using Wox.Infrastructure.Logger;
using Wox.Infrastructure.UserSettings;
using Wox.Plugin;
using Wox.Core.Storage;

namespace Wox.ViewModel
{
    public class ResultsViewModel : BaseModel
    {
        #region Private Fields

        private Dictionary<string, PluginQueryResult> pluginQueryResults = new();
        public ResultCollection Results { get; }

        private readonly Settings _settings;
        private int MaxResults => _settings?.MaxResultsToShow ?? 6;
        private readonly object _collectionLock = new object();

        public ResultsViewModel()
        {
            Results = new ResultCollection();
            BindingOperations.EnableCollectionSynchronization(Results, _collectionLock);

            SelectNextItemCommand = new RelayCommand(_ => this.SelectNextResult());
            SelectPrevItemCommand = new RelayCommand(_ => SelectPrevResult());
            SelectNextPageCommand = new RelayCommand(_ => SelectNextPage());
            SelectPrevPageCommand = new RelayCommand(_ => SelectPrevPage());
            SelectFirstResultCommand = new RelayCommand(_ => SelectFirstResult());
        }

        public ResultsViewModel(Settings settings) : this()
        {
            _settings = settings;
            _settings.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_settings.MaxResultsToShow))
                {
                    OnPropertyChanged(nameof(MaxHeight));
                }
            };
        }

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #endregion Private Fields

        public ICommand SelectNextItemCommand { get; set; }
        public ICommand SelectPrevItemCommand { get; set; }
        public ICommand SelectNextPageCommand { get; set; }
        public ICommand SelectPrevPageCommand { get; set; }
        public ICommand SelectFirstResultCommand { get; set; }

        public ICommand OpenResultCommand { get; set; } = new RelayCommand(obj => { });

        public ICommand LoadContextMenuCommand { get; set; } = new RelayCommand(obj => { });

        #region Properties

        public int MaxHeight => MaxResults * 50;

        public int SelectedIndex { get; set; }

        public ResultViewModel SelectedItem { get; set; }
        public Thickness Margin { get; set; }
        public Visibility Visbility { get; set; } = Visibility.Collapsed;

        public string PluginID { get; set; }

        #endregion Properties

        #region Private Methods

        private int NewIndex(int i)
        {
            var n = Results.Count;
            if (n > 0)
            {
                i = (n + i) % n;
                return i;
            }
            else
            {
                // SelectedIndex returns -1 if selection is empty.
                return -1;
            }
        }

        #endregion Private Methods

        #region Public Methods

        public void SelectNextResult()
        {
            SelectedIndex = NewIndex(SelectedIndex + 1);
        }

        public void SelectPrevResult()
        {
            SelectedIndex = NewIndex(SelectedIndex - 1);
        }

        public void SelectNextPage()
        {
            SelectedIndex = NewIndex(SelectedIndex + MaxResults);
        }

        public void SelectPrevPage()
        {
            SelectedIndex = NewIndex(SelectedIndex - MaxResults);
        }

        public void SelectFirstResult()
        {
            SelectedIndex = NewIndex(0);
        }

        public void Clear()
        {
            this.pluginQueryResults.Clear();
            Results.RemoveAll();
        }

        public int Count => Results.Count;

        public void AddResults(List<Result> newRawResults, string resultId)
        {
            List<PluginQueryResult> updates = new List<PluginQueryResult>()
            {
                new PluginQueryResult(newRawResults, resultId)
            };
            AddResults(updates, CancellationToken.None);

        }

        /// <summary>
        /// To avoid deadlock, this method should not called from main thread
        /// </summary>
        public void AddResults(IEnumerable<PluginQueryResult> updates, CancellationToken token)
        {
            // https://stackoverflow.com/questions/14336750

            // because IResultUpdated, updates maybe contains same plugin result
            // we just need the last one

            foreach (var update in updates)
            {
                this.pluginQueryResults[update.PluginID] = update;
            }

            var newResults = this.pluginQueryResults.Values
                .SelectMany(p => p.Results)
                .Select(p => new ResultViewModel(p))
                .OrderByDescending(r => r.Result.Score)
                .Take(MaxResults * 4);

            if (token.IsCancellationRequested)
                return;
            Results.Update(newResults);

            if (Results.Count > 0)
            {
                var id = Results[0].Result.PluginID;
                if (Results.All(p => p.Result.PluginID == id))
                    this.PluginID = id;
                else
                    this.PluginID = null;
            }
            else
            {
                this.PluginID = null;
            }

            this.Visbility = Results.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            //if (Results.Count > 0)
            //    SelectedIndex = 0;
        }

        public void Add(PluginQueryResult update)
        {
            // https://stackoverflow.com/questions/14336750
            lock (_collectionLock)
            {
                // because IResultUpdated, updates maybe contains same plugin result
                // we just need the last one

                var newResults = Results.ToList()
                    .Where(p => p.Result.PluginID != update.PluginID) // remove previous result
                    .Concat(update.Results.Select(r => new ResultViewModel(r)))
                    .OrderByDescending(r => r.Result.Score)
                    .Take(MaxResults * 4);

                Results.Update(newResults);
                if (Results.Count > 0)
                    SelectedIndex = 0;
            }
        }

        #endregion Public Methods

        public class ResultCollection : List<ResultViewModel>, INotifyCollectionChanged
        {
            public event NotifyCollectionChangedEventHandler CollectionChanged;

            public void RemoveAll()
            {
                this.Clear();
                if (CollectionChanged != null)
                {
                    CollectionChanged.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
            }

            public void Update(IEnumerable<ResultViewModel> newItems)
            {
                this.Clear();
                this.AddRange(newItems);

                if (CollectionChanged != null)
                {
                    // wpf use directx / double buffered already, so just reset all won't cause ui flickering
                    CollectionChanged.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
            }
        }
    }

    public class HistoryViewModel : ResultsViewModel
    {
        public HistoryViewModel(Settings settings) : base(settings)
        {
            var api = App.API;

            this.OpenResultCommand = new RelayCommand(index =>
            {
                if (index != null)
                {
                    SelectedIndex = int.Parse(index.ToString());
                }

                var result = SelectedItem?.Result;
                if (result != null)
                {
                    bool hideWindow = result.Action != null && result.Action(new ActionContext
                    {
                        SpecialKeyState = GlobalHotkey.Instance.CheckModifiers(),
                        API = App.API
                    });

                    if (hideWindow)
                    {
                        api.HideWindow();
                    }
                }
            });
        }
    }

    public class QueryResultViewModel : ResultsViewModel
    {
        public QueryResultViewModel(Settings settings, UserSelectedRecord record, History history) : base(settings)
        {
            var api = App.API;

            this.OpenResultCommand = new RelayCommand(index =>
            {
                if (index != null)
                {
                    SelectedIndex = int.Parse(index.ToString());
                }

                var result = SelectedItem?.Result;
                if (result != null)
                {
                    bool hideWindow = result.Action != null && result.Action(new ActionContext
                    {
                        SpecialKeyState = GlobalHotkey.Instance.CheckModifiers(),
                        API = App.API
                    });

                    if (hideWindow)
                    {
                        api.HideWindow();
                    }

                    record.Add(result);
                    history.Add(result.OriginQuery.RawQuery);
                }
            });
        }
    }

    public class ContextViewModel : ResultsViewModel
    {
        public ContextViewModel(Settings settings) : base(settings)
        {
            var api = App.API;

            this.OpenResultCommand = new RelayCommand(index =>
            {
                if (index != null)
                {
                    SelectedIndex = int.Parse(index.ToString());
                }

                var result = SelectedItem?.Result;
                if (result != null)
                {
                    bool hideWindow = result.Action != null && result.Action(new ActionContext
                    {
                        SpecialKeyState = GlobalHotkey.Instance.CheckModifiers(),
                        API = App.API
                    });

                    if (hideWindow)
                    {
                        api.HideWindow();
                    }
                }
            });
        }
    }
}