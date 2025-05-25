using NLog;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Wox.Core.Services;
using Wox.Infrastructure.UserSettings;

namespace Wox.ViewModel
{
    public class ResultsViewModel : ViewModelBase
    {
        //static Bitmap defaultResultIcon = new ImageIconLoader().
        #region Private Fields


        private Action Action;
        private readonly Settings _settings;
        public int MaxResults => _settings?.MaxResultsToShow ?? 6;

        public ResultsViewModel(Settings settings, Action action)
        {
            this.Action = action;

            _settings = settings;
            _settings.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_settings.MaxResultsToShow))
                {
                    this.RaisePropertyChanged(nameof(MaxHeight));
                    this.RaisePropertyChanged(nameof(MaxResults));
                }
            };
        }


        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #endregion Private Fields

        public ICommand OpenResultCommand { get; set; } = new RelayCommand(obj => { });

        public ICommand LoadContextMenuCommand { get; set; } = new RelayCommand(obj => { });

        #region Properties

        public int MaxHeight => MaxResults * 50;

        public ResultCollection Results { get; } = new();

        public int SelectedIndex { get; set => this.RaiseAndSetIfChanged(ref field, value); } = -1;

        public ResultViewModel? SelectedItem { get; set => this.RaiseAndSetIfChanged(ref field, value); }
        public Thickness Margin { get; set; }

        public bool IsVisible { get; set => this.RaiseAndSetIfChanged(ref field, value); }
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

        public int Count => Results.Count;

        private List<ResultViewModel>? CreateResults(IEnumerable<PluginQueryResult> updates)
        {
            var list = updates
                .SelectMany(item => item.Results.Select(p => new ResultViewModel(p, item)))
                .OrderByDescending(p => p.Score)
                .Take(MaxResults * 6)
                .ToList();

            return list.Count > 0 ? list : null;
        }

        /// <summary>
        /// To avoid deadlock, this method should not called from main thread
        /// </summary>
        public void SetResult(PluginQueryResult item)
        {
            SetResult([item]);
        }
        public void SetResult(IList<PluginQueryResult> items)
        {
            List<ResultViewModel> list = CreateResults(items) ?? new List<ResultViewModel>();
            this.Results.Update(list);
            if (list.Count > 0)
            {
                SelectedItem = list[0];
                SelectedIndex = 0;
            }
            this.Action();
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
            }
        }
    }
}