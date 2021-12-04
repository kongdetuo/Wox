using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using NLog;
using Wox.Infrastructure;
using Wox.Infrastructure.Logger;
using Microsoft.WindowsAPICodePack.Shell;
using Windows.ApplicationModel.Resources;
using System.Diagnostics.CodeAnalysis;

namespace Wox.Plugin.Program.Programs
{
    [Serializable]
    public class Win32 : IProgram
    {
        public string Name { get; set; }
        public string IcoPath { get; set; }
        public string FullPath { get; set; }
        public string ParentDirectory { get; set; }
        public string ExecutableName { get; set; }
        public bool Valid { get; set; }
        public bool Enabled { get; set; }
        public string Location => ParentDirectory;

        private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

        public Result Result(string query, IPublicAPI api)
        {
            var match = StringMatcher.FuzzySearch(query, Name);
            var result = new Result
            {
                Title = Name,
                SubTitle = "Win32 ”¶”√≥Ã–Ú",
                TitleHighlightData = match.MatchData,
                Score = match.Score,
                IcoPath = IcoPath,
                ContextData = this,
                Action = Actions.RunExe(FullPath, ParentDirectory)
            };

            return result;
        }


        public List<Result> ContextMenus(IPublicAPI api)
        {
            var contextMenus = new List<Result>
            {
                new Result
                {
                    Title = api.GetTranslation("wox_plugin_program_run_as_different_user"),
                    Action = Actions.RunAsDifferentUser(FullPath, ParentDirectory),
                    IcoPath = "Images/app.png"
                },
                new Result
                {
                    Title = api.GetTranslation("wox_plugin_program_run_as_administrator"),
                    Action = Actions.RunExeAsAdministrator(FullPath, ParentDirectory),
                    IcoPath = "Images/cmd.png"
                },
                new Result
                {
                    Title = api.GetTranslation("wox_plugin_program_open_containing_folder"),
                    Action = Actions.OpenDirectory(ParentDirectory),
                    IcoPath = "Images/folder.png"
                }
            };
            return contextMenus;
        }



        public override string ToString()
        {
            return ExecutableName;
        }

        private static Win32 Win32Program(string path)
        {
            try
            {
                var p = new Win32
                {
                    Name = Path.GetFileNameWithoutExtension(path),
                    IcoPath = path,
                    FullPath = path,
                    ParentDirectory = Directory.GetParent(path).FullName,
                    Valid = true,
                    Enabled = true
                };
                return p;
            }
            catch (Exception e) when (e is SecurityException || e is UnauthorizedAccessException)
            {
                Logger.WoxError($"Permission denied {path}");
                return new Win32() { Valid = false, Enabled = false };
            }
        }

        private static IEnumerable<string> ProgramPaths(string directory, string[] suffixes)
        {
            if (!Directory.Exists(directory))
                return Enumerable.Empty<string>();

            var options = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = true
            };

            var list = Directory.EnumerateFiles(directory, "*", options).Where(x => suffixes.Any(s => IsExtension(x, s)));
            return list;
        }


        private static bool IsExtension(string path, string extension)
        {
            return Path.GetExtension(path.AsSpan()).Slice(1).Equals(extension.AsSpan(), StringComparison.OrdinalIgnoreCase);
        }

        private static ParallelQuery<Win32> UnregisteredPrograms(List<ProgramSource> sources, string[] suffixes)
        {
            var paths = sources.Select(s => s.Location)
                               .Select(Environment.ExpandEnvironmentVariables)
                               .Where(Directory.Exists)
                               .SelectMany(location => ProgramPaths(location, suffixes));
            var programs = paths.AsParallel().Select(Win32Program);
            return programs;
        }

        private static ParallelQuery<Win32> StartMenuPrograms(string[] suffixes)
        {
            var directory1 = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            // some program is not inside program directory, e.g. docker desktop
            directory1 = Directory.GetParent(directory1).FullName;
            var directory2 = Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms);
            directory2 = Directory.GetParent(directory2).FullName;
            var paths1 = ProgramPaths(directory1, suffixes);
            var paths2 = ProgramPaths(directory2, suffixes);
            var paths = paths1.Concat(paths2);

            var programs = paths.AsParallel().Select(Win32Program);
            return programs;
        }

        private static ParallelQuery<Win32> AppPathsPrograms(string[] suffixes)
        {
            // https://msdn.microsoft.com/en-us/library/windows/desktop/ee872121
            const string appPaths = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths";
            var programs = new List<Win32>();
            using (var root = Registry.LocalMachine.OpenSubKey(appPaths))
            {
                if (root != null)
                {
                    programs.AddRange(GetProgramsFromRegistry(root));
                }
            }
            using (var root = Registry.CurrentUser.OpenSubKey(appPaths))
            {
                if (root != null)
                {
                    programs.AddRange(GetProgramsFromRegistry(root));
                }
            }

            var filtered = programs.AsParallel().Where(p => suffixes.Any(s => IsExtension(p.ExecutableName, s)));
            return filtered;
        }

        private static IEnumerable<Win32> GetProgramsFromRegistry(RegistryKey root)
        {
            return root
                    .GetSubKeyNames()
                    .Select(x => GetProgramPathFromRegistrySubKeys(root, x))
                    .Distinct()
                    .Select(x => GetProgramFromPath(x));
        }

        private static string GetProgramPathFromRegistrySubKeys(RegistryKey root, string subkey)
        {
            var path = string.Empty;
            try
            {
                using (var key = root.OpenSubKey(subkey))
                {
                    if (key == null)
                        return string.Empty;

                    var defaultValue = string.Empty;
                    path = key.GetValue(defaultValue) as string;
                }

                if (string.IsNullOrEmpty(path))
                    return string.Empty;

                // fix path like this: ""\"C:\\folder\\executable.exe\""
                return path = path.Trim('"', ' ');
            }
            catch (Exception e) when (e is SecurityException || e is UnauthorizedAccessException)
            {
                Logger.WoxError($"Permission denied {root.ToString()} {subkey}");
                return string.Empty;
            }
        }

        private static Win32 GetProgramFromPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return new Win32();

            path = Environment.ExpandEnvironmentVariables(path);

            if (!File.Exists(path))
                return new Win32();

            var entry = Win32Program(path);
            entry.ExecutableName = Path.GetFileName(path);

            return entry;
        }

        public static Win32[] All(Settings settings)
        {

            var programs = new List<Win32>().AsParallel();
            try
            {
                var unregistered = UnregisteredPrograms(settings.ProgramSources, settings.ProgramSuffixes);
                programs = programs.Concat(unregistered);
            }
            catch (Exception e)
            {
                Logger.WoxError("Cannot read win32", e);
                return new Win32[] { };
            }

            try
            {
                if (settings.EnableRegistrySource)
                {
                    var appPaths = AppPathsPrograms(settings.ProgramSuffixes);
                    programs = programs.Concat(appPaths);
                }
            }
            catch (Exception e)
            {
                Logger.WoxError("Cannot read win32", e);
                return new Win32[] { };
            }

            try
            {
                if (settings.EnableStartMenuSource)
                {
                    var startMenu = StartMenuPrograms(settings.ProgramSuffixes);
                    programs = programs.Concat(startMenu);
                }
            }
            catch (Exception e)
            {
                Logger.WoxError("Cannot read win32", e);
                return new Win32[] { };
            }
            return programs.ToArray();

        }
    }
}
