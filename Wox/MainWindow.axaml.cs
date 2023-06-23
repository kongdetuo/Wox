using System;
using System.ComponentModel;
using NLog;
using Wox.Core.Resource;
using Wox.Infrastructure.UserSettings;
using Wox.ViewModel;
using NotifyIcon = System.Windows.Forms.NotifyIcon;
using System.Reactive.Linq;
using Wox.Themes;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using System.Windows;
using ReactiveUI;
using System.Windows.Input;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using System.Reactive;

namespace Wox
{
    public partial class MainWindow : ReactiveWindow<MainViewModel>
    {
        //private readonly Storyboard _progressBarStoryboard = new();
        private Settings _settings = null!;


        public MainWindow(MainViewModel mainVM) : this()
        {
            DataContext = mainVM;
            ViewModel = mainVM;

  
            ViewModel.WhenAnyValue(p => p.ShowMainWindow).Where(p=>p).Subscribe(p =>
            {
                if (p)
                {
                    UpdatePosition();
                    Show();
                    Activate();
                    _settings.ActivateTimes++;
                    if (!ViewModel.LastQuerySelected)
                    {
                        QueryTextBox.SelectAll();
                        ViewModel.LastQuerySelected = true;
                    }
                }
                else
                {
                    Hide();
                }
            });

            //ViewModel1.WhenChanged(p => p.ShowProcessBar)
            //    .Subscribe(p =>
            //    {
            //        //if (p)
            //        //{
            //        //    this.Dispatcher.Invoke(() => ProgressBar.BeginStoryboard(_progressBarStoryboard));
            //        //}
            //        //else
            //        //{
            //        //    this.Dispatcher.Invoke(() => _progressBarStoryboard.Stop(ProgressBar));
            //        //}
            //    });

            this.ViewModel!.ShowSettingInteraction.RegisterHandler(context =>
            {
                this.ViewModel.ShowMainWindow = false;
                var settingView = new SettingWindow(App.API, new SettingWindowViewModel());
                settingView.Show();

                context.SetOutput(Unit.Default);
            });

        }



        public MainWindow()
        {
            InitializeComponent();

            _settings = Settings.Instance;
            this.PointerPressed += OnPointerPressed;
            this.Loaded += OnLoaded;
            this.PositionChanged += MainWindow_PositionChanged;
        }


        private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs _)
        {
            InitProgressbarAnimation();
            InitializePosition();
        }

        private void B_ContextRequested(object? sender, ContextRequestedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void InitializePosition()
        {

            //Top = WindowTop();
            //Left = WindowLeft();
            //_settings.WindowTop = Top;
            //_settings.WindowLeft = Left;
        }

        private void InitProgressbarAnimation()
        {
            //var da = new DoubleAnimation(ProgressBar.X2, ActualWidth + 100, new Duration(new TimeSpan(0, 0, 0, 0, 1600)));
            //var da1 = new DoubleAnimation(ProgressBar.X1, ActualWidth, new Duration(new TimeSpan(0, 0, 0, 0, 1600)));
            //Storyboard.SetTargetProperty(da, new PropertyPath("(Line.X2)"));
            //Storyboard.SetTargetProperty(da1, new PropertyPath("(Line.X1)"));
            //_progressBarStoryboard.Children.Add(da);
            //_progressBarStoryboard.Children.Add(da1);
            //_progressBarStoryboard.RepeatBehavior = RepeatBehavior.Forever;
            //_viewModel.ProgressBarVisibility = Visibility.Hidden;
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            this.BeginMoveDrag(e);
        }

        //private void OnDrop(object sender, DragEventArgs e)
        //{
        //    if (e.Data.GetDataPresent(DataFormats.FileDrop))
        //    {
        //        // Note that you can have more than one file.
        //        string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
        //        if (files[0].ToLower().EndsWith(".wox"))
        //        {
        //            PluginManager.InstallPlugin(files[0]);
        //        }
        //        else
        //        {
        //            MessageBox.Show(InternationalizationManager.Instance.GetTranslation("invalidWoxPluginFileFormat"));
        //        }
        //    }
        //    e.Handled = false;
        //}

        //private void OnPreviewDragOver(object sender, DragEventArgs e)
        //{
        //    e.Handled = true;
        //}

        //private void OnContextMenusForSettingsClick(object sender, RoutedEventArgs e)
        //{
        //    App.API.OpenSettingDialog();
        //}
        private void Window_Activated(object? sender, EventArgs e)
        {
            ThemeManager.Instance.SetBlurForWindow();
            QueryTextBox.Focus();
        }

        private void Window_Deactivated(object? sender, EventArgs e)
        {
            ThemeManager.Instance.DisableBlur();
            if (_settings.HideWhenDeactive)
            {
                this.ViewModel!.ShowMainWindow = false;
            }
        }

        private void UpdatePosition()
        {
            if (_settings.RememberLastLaunchLocation)
            {
                var x = Convert.ToInt32(_settings.WindowLeft);
                var y = Convert.ToInt32(_settings.WindowTop);
                Position = new Avalonia.PixelPoint(x, y);
            }
            else
            {
                var screen = Screens.Primary;
                var screenHeight = screen.WorkingArea.Height;
                var screenWidth = screen.WorkingArea.Width;
                var screenCenterX = screen.WorkingArea.Right - screen.WorkingArea.Width / 2;
                var screenCenterY = screen.WorkingArea.Bottom - screen.WorkingArea.Height / 2;


                var left = screenCenterX - this.Width / 2;
                var top = screenCenterY - screenHeight / 4;
                Position = new Avalonia.PixelPoint(Convert.ToInt32(left), Convert.ToInt32(top));
            }
        }

        private void MainWindow_PositionChanged(object? sender, PixelPointEventArgs e)
        {
            if (_settings.RememberLastLaunchLocation)
            {
                _settings.WindowLeft = e.Point.X;
                _settings.WindowTop = e.Point.Y;
            }
        }
    }
}