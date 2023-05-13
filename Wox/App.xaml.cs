using System;
using System.Windows;
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

namespace Wox
{
    public partial class App : IDisposable, ISingleInstanceApp
    {
        public static PublicAPIInstance API { get; private set; }
        private const string Unique = "Wox_Unique_Application_Mutex";
        private static bool _disposed;
        private MainViewModel _mainVM;
        private SettingWindowViewModel _settingsVM;
        private StringMatcher _stringMatcher;
        private static string _systemLanguage;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [STAThread]
        public static void Main()
        {
            if (SingleInstance<App>.InitializeAsFirstInstance(Unique))
            {
                using var application = new App();
                application.InitializeComponent();
                application.Run();
            }
        }

        private async void OnStartup(object sender, StartupEventArgs e)
        {
            var time = await Logger.StopWatchNormal("Startup cost", async () =>
            {
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
                var window = new MainWindow(_mainVM);
                API = new PublicAPIInstance(_settingsVM, _mainVM);
                await PluginManager.InitializePluginsAsync(API);

                Current.MainWindow = window;
                Current.MainWindow.Title = Constant.Wox;

                // todo temp fix for instance code logic
                // load plugin before change language, because plugin language also needs be changed
                InternationalizationManager.Instance.Settings = Settings.Instance;
                InternationalizationManager.Instance.ChangeLanguage(Settings.Instance.Language);
                // main windows needs initialized before theme change because of blur settigns
                ThemeManager.Instance.ChangeTheme(Settings.Instance.Theme);

                Http.Proxy = Settings.Instance.Proxy;

                RegisterExitEvents();

                AutoStartup();

                _mainVM.MainWindowVisibility = Settings.Instance.HideOnStartup ? Visibility.Hidden : Visibility.Visible;

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
            Current.Exit += (s, e) => Dispose();
            Current.SessionEnding += (s, e) => Dispose();
        }

        /// <summary>
        /// let exception throw as normal is better for Debug
        /// </summary>
        //[Conditional("RELEASE")]
        private void RegisterDispatcherUnhandledException()
        {
            DispatcherUnhandledException += ErrorReporting.DispatcherUnhandledException;
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
            Current.MainWindow.Visibility = Visibility.Visible;
        }
    }
}