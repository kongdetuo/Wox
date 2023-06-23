using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml.Templates;

namespace Wox
{
    public partial class ResultsView : UserControl
    {
        public ResultsView()
        {
            InitializeComponent();
        }

        private void OpenResult(object? sender, TappedEventArgs e)
        {
            var mainWindow = TopLevel.GetTopLevel(this) as MainWindow;
            var mainVM = mainWindow?.ViewModel;
            if (mainVM != null)
            {
                mainVM.OpenResultCommand.Execute((sender as Control)!.DataContext);
            }
        }
        private void OpenContext(object? sender, ContextRequestedEventArgs e)
        {
            var mainWindow = TopLevel.GetTopLevel(this) as MainWindow;
            var mainVM = mainWindow?.ViewModel;
            if (mainVM != null)
            {
                mainVM.LoadContextMenuCommand.Execute((sender as Control)!.DataContext);
            }
        }

        private void OnPointerMoved(object sender, PointerEventArgs e)
        {
            var item = (sender as Control)!.Parent!.Parent as ListBoxItem;
            if (item != null)
            {
                item.IsSelected = true;
            }
        }
    }
}