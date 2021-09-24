using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using Wox.Infrastructure;

namespace Wox.Plugin
{
    public class Actions
    {
        public static IPublicAPI API { get; set; }

        public static Func<ActionContext, bool> CopyTextToClipboard(string value)
        {
            return context =>
            {
                try
                {
                    Clipboard.SetText(value);
                    return true;
                }
                catch (ExternalException)
                {
                    API.ShowMsg("Copy failed, please try later");
                    return false;
                }
            };
        }

        public static Func<ActionContext, bool> CopyFilesToClipboard(params string[] filepaths)
        {
            return _ =>
            {
                var collect = new System.Collections.Specialized.StringCollection();
                collect.AddRange(filepaths);
                Clipboard.SetFileDropList(collect);
                return true;
            };
        }

        public static Func<ActionContext, bool> StartNewQuery(string queryText)
        {
            return context =>
            {
                API.ChangeQuery(queryText, true);
                return false;
            };
        }

        public static Func<ActionContext, bool> OpenFile(string filePath)
        {
            return context =>
            {
                Process.Start(ShellCommand.SetProcessStartInfo(filePath));
                return true;
            };
        }

        public static Func<ActionContext, bool> Run(string filePath, bool runAs)
        {
            return _ =>
            {
                var info = new ProcessStartInfo
                {
                    FileName = filePath,
                    WorkingDirectory = Path.GetDirectoryName(filePath),
                    Verb = runAs ? "runas" : ""
                };

                try
                {
                    Process.Start(info);
                }
                catch (Exception)
                {
                    var name = "Plugin: Program";
                    var message = $"Unable to start: {info.FileName}";
                    API.ShowMsg(name, message, string.Empty);
                }
                return true;
            };
        }

        public static Func<ActionContext, bool> OpenDirectory(string filePath)
        {
            return context =>
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
                return true;
            };
        }

        public static Func<ActionContext, bool> OpenInNewBrowserWindow(string url, string browserPath = "")
        {
            return context =>
            {
                SearchWeb.NewBrowserWindow(url, browserPath);
                return true;
            };
        }

        public static Func<ActionContext, bool> OpenInNewBrowserTab(string url, string browserPath = "")
        {
            return context =>
            {
                SearchWeb.NewTabInBrowser(url, browserPath);
                return true;
            };
        }

        public static Func<ActionContext, bool> Empty(bool hideWindow = false)
        {
            return _ => hideWindow;
        }
    }
}
