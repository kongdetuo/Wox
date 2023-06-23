using System;
//using System.Windows;
using System.Globalization;
using NLog;

using Wox.Core.Plugin;
using Wox.Core.Resource;
using Wox.Helper;
using Wox.Infrastructure;
using Wox.Infrastructure.Http;
using Wox.Image;
using Wox.Infrastructure.Logger;
using Wox.Infrastructure.UserSettings;
using Wox.ViewModel;
using Wox.Infrastructure.Exception;
//using Sentry;
using Wox.Themes;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Shapes;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Tmds.DBus.SourceGenerator;
using Avalonia.Markup.Xaml;
using System.Threading.Tasks;
using Avalonia.ReactiveUI;
using Avalonia.Styling;
using Avalonia.Controls;

namespace Wox
{


    public partial class App : Application, IDisposable, ISingleInstanceApp
    {
        public static PublicAPIInstance API { get; private set; } = null!;
        private static bool _disposed;
        private MainViewModel _mainVM = null!;
        private SettingWindowViewModel _settingsVM = null!;
        private StringMatcher _stringMatcher = null!;
        private static string _systemLanguage = null!;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();



        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
        public override async void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                await OnStartup(desktop);
            }
            base.OnFrameworkInitializationCompleted();
        }

        private static ThemeVariant Dark = new ThemeVariant("Dark", null);

        private async Task OnStartup(IClassicDesktopStyleApplicationLifetime desktop)
        {
            var time = await Logger.StopWatchNormal("Startup cost", async () =>
            {
                this.RequestedThemeVariant = Dark;

                _systemLanguage = CultureInfo.CurrentUICulture.Name;
                RegisterAppDomainExceptions();
                RegisterDispatcherUnhandledException();

                Logger.WoxInfo("Begin Wox startup----------------------------------------------------");
                Settings.Initialize();
                ExceptionFormatter.Initialize(_systemLanguage, Settings.Instance.Language);
                InsertWoxLanguageIntoLog();

                Logger.WoxInfo(ExceptionFormatter.RuntimeInfo());

                ImageLoader.Initialize();

                _settingsVM = new SettingWindowViewModel();

                _stringMatcher = new StringMatcher();
                StringMatcher.Instance = _stringMatcher;
                _stringMatcher.UserSettingSearchPrecision = Settings.Instance.QuerySearchPrecision;

                PluginManager.LoadPlugins(Settings.Instance.PluginSettings);
                _mainVM = new MainViewModel();

                //desktop.MainWindow = new MainWindow
                //{
                //    IsVisible = false,
                //    // DataContext = new MainViewModel()
                //};
                //var window = desktop.MainWindow as ReactiveWindow<MainViewModel>;// new MainWindow() {  };
                // window.DataContext = _mainVM;
                var window = new MainWindow(_mainVM)
                {
                    IsVisible = false,
                };
                window.ViewModel = _mainVM;
                API = new PublicAPIInstance(_settingsVM, _mainVM);
                this.DataContext = new AppViewModel(_mainVM, desktop);

                await PluginManager.InitializePluginsAsync(API);

                // desktop.MainWindow= window; 
                //Current.MainWindow = window;
                //Current.MainWindow.Title = Constant.Wox;

                // todo temp fix for instance code logic
                // load plugin before change language, because plugin language also needs be changed
                InternationalizationManager.Instance.Settings = Settings.Instance;
                InternationalizationManager.Instance.ChangeLanguage(Settings.Instance.Language);
                // main windows needs initialized before theme change because of blur settigns
                //ThemeManager.Instance.ChangeTheme(Settings.Instance.Theme);

                //Http.Proxy = Settings.Instance.Proxy;

                //RegisterExitEvents();

                //AutoStartup();

                if (Settings.Instance.HideOnStartup == false)
                    _mainVM.ShowMainWindow = true;
                //TrayIcon
                Logger.WoxInfo($"SDK Info: {ExceptionFormatter.SDKInfo()}");
                Logger.WoxInfo("End Wox startup ----------------------------------------------------  ");
            });
        }



        private static void InsertWoxLanguageIntoLog()
        {
            Log.updateSettingsInfo(Settings.Instance.Language);
            Settings.Instance.PropertyChanged += (s, ev) =>
            {
                if (ev.PropertyName == nameof(Settings.Instance.Language))
                {
                    Log.updateSettingsInfo(Settings.Instance.Language);
                }
            };
        }

        private void AutoStartup()
        {
            if (Settings.Instance.StartWoxOnSystemStartup)
            {
                if (!SettingWindow.StartupSet())
                {
                    SettingWindow.SetStartup();
                }
            }
        }

        private void RegisterExitEvents()
        {
            AppDomain.CurrentDomain.ProcessExit += (s, e) => Dispose();
            //Current.Exit += (s, e) => Dispose();
            //Current.SessionEnding += (s, e) => Dispose();
        }

        /// <summary>
        /// let exception throw as normal is better for Debug
        /// </summary>
        //[Conditional("RELEASE")]
        private void RegisterDispatcherUnhandledException()
        {
            //DispatcherUnhandledException += ErrorReporting.DispatcherUnhandledException;
        }

        /// <summary>
        /// let exception throw as normal is better for Debug
        /// </summary>
        //[Conditional("RELEASE")]
        private static void RegisterAppDomainExceptions()
        {
            AppDomain.CurrentDomain.UnhandledException += ErrorReporting.UnhandledExceptionHandleMain;
        }

        public void Dispose()
        {
            Logger.WoxInfo("Wox Start Displose");
            // if sessionending is called, exit proverbially be called when log off / shutdown
            // but if sessionending is not called, exit won't be called when log off / shutdown
            if (!_disposed)
            {
                API?.SaveAppAllSettings();
                _disposed = true;
                // todo temp fix to exist application
                // should notify child thread programmaly
                Environment.Exit(0);
            }
            Logger.WoxInfo("Wox End Displose");
        }

        public void OnSecondAppStarted()
        {
            // Current.MainWindow.Visibility = Visibility.Visible;
        }
    }

    class AppViewModel
    {
        public MainViewModel Main { get; private set; }
        public IClassicDesktopStyleApplicationLifetime LiftTime { get; private set; }

        public AppViewModel(MainViewModel main, IClassicDesktopStyleApplicationLifetime lifetime)
        {
            this.Main = main;
            this.LiftTime = lifetime;
        }
        public void OpenSetting()
        {
            this.Main.OpenSetting();
        }

        public void Awake()
        {
            this.Main.ShowMainWindow = true;

        }

        public void Shutdown()
        {
            this.LiftTime.Shutdown();
        }
    }
}