using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using Wox.Plugin.Program.Programs;
using System.ComponentModel;
using System.Windows.Data;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Wox.Plugin.Program.Views
{
    /// <summary>
    /// Interaction logic for ProgramSetting.xaml
    /// </summary>
    public partial class ProgramSetting : Avalonia.Controls.UserControl
    {
        private readonly PluginInitContext context;
        private readonly Settings _settings;
        private GridViewColumnHeader _lastHeaderClicked;
        private ListSortDirection _lastDirection;

        public ProgramSetting(PluginInitContext context, Settings settings)
        {
            this.context = context;
            InitializeComponent();
            Loaded += Setting_Loaded;
            _settings = settings;


        }

        private void Setting_Loaded(object sender, RoutedEventArgs e)
        {
            //programSourceView.ItemsSource = _settings.ProgramSources;
            //programIgnoreView.ItemsSource = _settings.IgnoredSequence;
            StartMenuEnabled.IsChecked = _settings.EnableStartMenuSource;
            RegistryEnabled.IsChecked = _settings.EnableRegistrySource;
        }

        private void ReIndexing()
        {
            //programSourceView.Items.Refresh();
            //Task.Run(() =>
            //{
            //    Dispatcher.Invoke(() => { indexingPanel.Visibility = Visibility.Visible; });
            //    Main.IndexPrograms();
            //    Dispatcher.Invoke(() => { indexingPanel.Visibility = Visibility.Hidden; });
            //});
        }

        private async void btnAddProgramSource_OnClick(object sender, RoutedEventArgs e)
        {
            var add = new AddProgramSource(context, _settings);
            Window? window = TopLevel.GetTopLevel(this) as Window;
            if (await add.ShowDialog<bool?>(window!) ?? false)
            {
                ReIndexing();
            }
        }

        private async void btnEditProgramSource_OnClick(object sender, RoutedEventArgs e)
        {
            //if (programSourceView.SelectedItem is ProgramSource selectedProgramSource)
            //{
            //    var add = new AddProgramSource(selectedProgramSource, _settings);
            //    Window? window = TopLevel.GetTopLevel(this) as Window;
            //    if (await add.ShowDialog<bool?>(window!) ?? false)
            //    {
            //        ReIndexing();
            //    }
            //}
            //else
            //{
            //    string msg = context.API.GetTranslation("wox_plugin_program_pls_select_program_source");
            //    //MessageBox.Show(msg);
            //}
        }

        private void btnReindex_Click(object sender, RoutedEventArgs e)
        {
            ReIndexing();
        }

        private async void BtnProgramSuffixes_OnClick(object sender, RoutedEventArgs e)
        {
            var p = new ProgramSuffixes(context, _settings);
            Window? window = TopLevel.GetTopLevel(this) as Window;
            if (await p.ShowDialog<bool?>(window!) ?? false)
            {
                ReIndexing();
            }
        }

        //private void programSourceView_DragEnter(object sender, DragEventArgs e)
        //{
        //    if (e.Data.GetDataPresent(DataFormats.FileDrop))
        //    {
        //        e.Effects = DragDropEffects.Link;
        //    }
        //    else
        //    {
        //        e.Effects = DragDropEffects.None;
        //    }
        //}

        //private void programSourceView_Drop(object sender, DragEventArgs e)
        //{
        //    var directories = (string[])e.Data.GetData(DataFormats.FileDrop);

        //    var directoriesToAdd = new List<ProgramSource>();

        //    if (directories != null && directories.Length > 0)
        //    {
        //        foreach (string directory in directories)
        //        {
        //            if (Directory.Exists(directory))
        //            {
        //                var source = new ProgramSource
        //                {
        //                    Location = directory,
        //                };

        //                directoriesToAdd.Add(source);
        //            }
        //        }

        //        if (directoriesToAdd.Count > 0)
        //        {
        //            directoriesToAdd.ForEach(x => _settings.ProgramSources.Add(x));
        //            ReIndexing();
        //        }
        //    }
        //}

        private void StartMenuEnabled_Click(object sender, RoutedEventArgs e)
        {
            _settings.EnableStartMenuSource = StartMenuEnabled.IsChecked ?? false;
            ReIndexing();
        }

        private void RegistryEnabled_Click(object sender, RoutedEventArgs e)
        {
            _settings.EnableRegistrySource = RegistryEnabled.IsChecked ?? false;
            ReIndexing();
        }

        private void btnProgramSoureDelete_OnClick(object sender, RoutedEventArgs e)
        {
            //var selectedItems = programSourceView
            //                    .SelectedItems.Cast<ProgramSource>()
            //                    .ToList();

            //if (selectedItems.Count == 0)
            //{
            //    string msg = context.API.GetTranslation("wox_plugin_program_pls_select_program_source");
            //    //MessageBox.Show(msg);
            //    context.API.ShowMsg(msg);
            //    return;
            //}
            //else
            //{
            //    _settings.ProgramSources.RemoveAll(s => selectedItems.Contains(s));
            //    programSourceView.SelectedItems.Clear();
            //    ReIndexing();
            //}
        }

        private void ProgramSourceView_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            //programSourceView.SelectedItems.Clear();
        }

        private void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            //ListSortDirection direction;

            //if (e.OriginalSource is GridViewColumnHeader headerClicked)
            //{
            //    if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
            //    {
            //        if (headerClicked != _lastHeaderClicked)
            //        {
            //            direction = ListSortDirection.Ascending;
            //        }
            //        else
            //        {
            //            if (_lastDirection == ListSortDirection.Ascending)
            //            {
            //                direction = ListSortDirection.Descending;
            //            }
            //            else
            //            {
            //                direction = ListSortDirection.Ascending;
            //            }
            //        }

            //        var columnBinding = headerClicked.Column.DisplayMemberBinding as Binding;
            //        var sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;

            //        Sort(sortBy, direction);

            //        _lastHeaderClicked = headerClicked;
            //        _lastDirection = direction;
            //    }
            //}
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            //var dataView = CollectionViewSource.GetDefaultView(programSourceView.ItemsSource);

            //dataView.SortDescriptions.Clear();
            //SortDescription sd = new SortDescription(sortBy, direction);
            //dataView.SortDescriptions.Add(sd);
            //dataView.Refresh();
        }

        private void btnDeleteIgnored_OnClick(object sender, RoutedEventArgs e)
        {
            //if (programIgnoreView.SelectedItem is IgnoredEntry selectedIgnoredEntry)
            //{
            //    string msg = string.Format(context.API.GetTranslation("wox_plugin_program_delete_ignored"), selectedIgnoredEntry);

            //    if (MessageBox.Show(msg, string.Empty, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            //    {
            //        _settings.IgnoredSequence.Remove(selectedIgnoredEntry);
            //        programIgnoreView.Items.Refresh();
            //    }
            //}
            //else
            //{
            //    string msg = context.API.GetTranslation("wox_plugin_program_pls_select_ignored");
            //    MessageBox.Show(msg);
            //}
        }

        private async void btnEditIgnored_OnClick(object sender, RoutedEventArgs e)
        {
            //if (programIgnoreView.SelectedItem is IgnoredEntry selectedIgnoredEntry)
            //{
            //    Window? window = TopLevel.GetTopLevel(this) as Window;
            //    await new AddIgnored(selectedIgnoredEntry, _settings).ShowDialog(window);
            //    //programIgnoreView.Items.Refresh();
            //}
            //else
            //{
            //    string msg = context.API.GetTranslation("wox_plugin_program_pls_select_ignored");
            //    MessageBox.Show(msg);
            //}
        }
        private async void btnAddIgnored_OnClick(object sender, RoutedEventArgs e)
        {
            //Window? window = TopLevel.GetTopLevel(this) as Window;

            //await new AddIgnored(_settings).ShowDialog(window!);
            //programIgnoreView.Items.Refresh();
        }
    }
}