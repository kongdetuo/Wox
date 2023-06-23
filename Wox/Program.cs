using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Wox.Helper;

namespace Wox
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            
            const string Unique = "Wox_Unique_Application_Mutex";
            if (SingleInstance<App>.InitializeAsFirstInstance(Unique))
            {
                BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args, ShutdownMode.OnExplicitShutdown);
            }
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI();
    }

    // todo 本地化
    // todo 主界面
    // todo 插件界面
    // todo 主题
}
