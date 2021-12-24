using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using Wox.Infrastructure;

namespace Wox.Plugin
{
    public class Actions
    {
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
                    context.API.ShowMsg("Copy failed, please try later");
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
                context.API.ChangeQuery(queryText, true);
                return false;
            };
        }

        public static Func<ActionContext, bool> RunExe(string filePath, string workingDirectory = "", bool runasWhenCtrlKeyDown = true)
        {
            return context =>
            {
                try
                {
                    if (runasWhenCtrlKeyDown && context.SpecialKeyState.CtrlPressed)
                        Process.Start(ShellCommand.SetProcessStartInfo(filePath, workingDirectory, "", "runas"));
                    else
                        Process.Start(ShellCommand.SetProcessStartInfo(filePath, workingDirectory));
                    return true;
                }
                catch (Exception)
                {
                    var message = "Error";
                    context.API.ShowMsg(message, $"Can't Start {filePath}");

                    return false;
                }
            };
        }

        public static Func<ActionContext, bool> RunExeAsAdministrator(string filePath, string workingDirectory = "")
        {
            return context =>
            {
                try
                {
                    Process.Start(ShellCommand.SetProcessStartInfo(filePath, workingDirectory, "", "runas"));
                    return true;
                }
                catch (Exception)
                {
                    var message = "Error";
                    context.API.ShowMsg(message, $"Can't Start {filePath}");

                    return false;
                }
            };
        }

        public static Func<ActionContext, bool> RunAsDifferentUser(string filePath, string workingDirectiory)
        {
            return context =>
            {
                var info = ShellCommand.SetProcessStartInfo(filePath, workingDirectiory);

                Task.Run(() =>
                {
                    try
                    {
                        ShellCommand.RunAsDifferentUser(info);
                    }
                    catch (Exception)
                    {
                        context.API.ShowMsg("Error");
                    }
                });

                return true;
            };
        }

        public static Func<ActionContext, bool> OpenFile(string filePath, string workingDir = "")
        {
            return context =>
            {
                try
                {
                    Process.Start(ShellCommand.SetProcessStartInfo(filePath, workingDir));
                    return true;
                }
                catch (Exception)
                {
                    var message = "Can't open this file";
                    context.API.ShowMsg(message);
                    return false;
                }
            };
        }

        public static Func<ActionContext, bool> Run(string filePath, bool runAs)
        {
            return context =>
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
                    context.API.ShowMsg(name, message, string.Empty);
                }
                return true;
            };
        }

        public static Func<ActionContext, bool> OpenDirectory(string filePath)
        {
            return context =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    var message = $"Can't open {filePath}";
                    context.API.ShowMsg(message, ex.Message);
                }

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