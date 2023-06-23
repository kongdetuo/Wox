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
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using MS.WindowsAPICodePack.Internal;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Alias;
using System.Reactive.Linq;
using DynamicData.Binding;
using System.Reactive.Concurrency;
using System.Diagnostics;
using Windows.ApplicationModel.Activation;

namespace Wox.ViewModel
{
    public class ResultsViewModel : ViewModelBase
    {

        #region Private Fields

        private Dictionary<string, List<ResultViewModel>> pluginQueryResults = new();
        public ResultCollection Results { get; }

        private readonly Settings _settings;
        private int MaxResults => _settings?.MaxResultsToShow ?? 6;
        private readonly object _collectionLock = new();

        public event EventHandler<List<PluginQueryResult>> resultchanged;

        public ResultsViewModel(Settings settings, Action action)
        {
            Results = new ResultCollection();
            // BindingOperations.EnableCollectionSynchronization(Results, _collectionLock);


            _settings = settings;
            _settings.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_settings.MaxResultsToShow))
                {
                    this.RaisePropertyChanged(nameof(MaxHeight));
                }
            };

            Observable.FromEventPattern<List<PluginQueryResult>>(this, nameof(resultchanged))
                .Select(p => p.EventArgs)
                .ObserveOn(ThreadPoolScheduler.Instance)
                .Select(createResults)
                //.Throttle(TimeSpan.FromMilliseconds(20))
                .ObserveOn(SynchronizationContext.Current!)
                .Subscribe(list =>
                {

                    Results.Update(list);

                    if (list.Count > 0)
                    {
                        SelectedItem = list[0];
                        SelectedIndex = 0;
                    }
                    action();
                });



        }


        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #endregion Private Fields

        public ICommand OpenResultCommand { get; set; } = new RelayCommand(obj => { });

        public ICommand LoadContextMenuCommand { get; set; } = new RelayCommand(obj => { });

        #region Properties

        public int MaxHeight => MaxResults * 50;

        [Reactive] public int SelectedIndex { get; set; } = -1;

        [Reactive] public ResultViewModel? SelectedItem { get; set; }
        public Thickness Margin { get; set; }

        [Reactive] public bool IsVisible { get; set; }
        public string? PluginID { get; set; }

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

        public void SelectNextItem()
        {
            SelectedIndex = NewIndex(SelectedIndex + 1);
        }

        public void SelectPrevItem()
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

        public void SelectFirstItem()
        {
            SelectedIndex = NewIndex(0);
        }

        public void Clear()
        {
            //this.cache.Clear();
            //Results.Clear();
        }

        public int Count => Results.Count;

        public void AddResults(List<Result> newRawResults, string resultId)
        {
            List<PluginQueryResult> updates = new()
            {
                new PluginQueryResult(newRawResults, resultId)
            };
            AddResults(updates, CancellationToken.None);

        }

        private List<ResultViewModel> createResults(IEnumerable<PluginQueryResult> updates)
        {

            foreach (var item in updates)
            {
                pluginQueryResults[item.PluginID] = item.Results.Select(p => new ResultViewModel(p, item)).ToList();
            }
            var list = pluginQueryResults.Values
                .SelectMany(p => p).OrderByDescending(p => p.Score)
                .Take(MaxResults * 6)
                .ToList();

            return list;

        }

        /// <summary>
        /// To avoid deadlock, this method should not called from main thread
        /// </summary>
        public void AddResults(IEnumerable<PluginQueryResult> updates, CancellationToken token)
        {
            resultchanged?.Invoke(this, updates.ToList());
            return;
            lock (_collectionLock)
            {
                IEnumerable<ResultViewModel> Create(PluginQueryResult rs) => rs.Results.Select(p => new ResultViewModel(p, rs));

                // https://stackoverflow.com/questions/14336750
                // because IResultUpdated, updates maybe contains same plugin result
                // we just need the last one
                // this.SelectedItem = null;

                foreach (var update in updates)
                {
                    this.pluginQueryResults[update.PluginID] = Create(update).ToList();
                }

                var newResults = this.pluginQueryResults.Values
                    .SelectMany(p => p)
                    .OrderByDescending(r => r.Score)
                    .Take(MaxResults * 6)
                    .ToList();

                if (token.IsCancellationRequested)
                    return;
                Results.Update(newResults);


            }
            //if (Results.Count > 0)
            //{
            //    this.SelectedItem = Results[0];
            //    this.SelectedIndex = 0;
            //    var id = Results[0].PluginMetadata?.ID;
            //    if (id != null && Results.All(p => p.PluginMetadata?.ID == id))
            //        this.PluginID = id;
            //    else
            //        this.PluginID = null;
            //}
            //else
            //{
            //    this.PluginID = null;
            //}
        }

        #endregion Public Methods

        public class ResultCollection : List<ResultViewModel>, INotifyCollectionChanged
        {
            public event NotifyCollectionChangedEventHandler? CollectionChanged;

            public void RemoveAll()
            {
                this.Clear();
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }

            public void Update(IList<ResultViewModel> newItems)
            {
                this.Clear();
                this.AddRange(newItems);
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                // wpf use directx / double buffered already, so just reset all won't cause ui flickering
            }
        }
    }
}