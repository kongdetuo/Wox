﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using NLog;
using Wox.Core.Resource;
using Wox.Infrastructure;
using Wox.Infrastructure.Logger;
using Wox.Infrastructure.UserSettings;

namespace Wox.Themes
{
    public class Theme
    {
        private readonly List<string> _themeDirectories = new List<string>();
        private ResourceDictionary _oldResource;
        public Settings Settings { get; set; }
        private const string Folder = "Themes";
        private const string Extension = ".xaml";
        private string DirectoryPath => Path.Combine(Constant.ProgramDirectory, Folder);
        private string UserDirectoryPath => Path.Combine(DataLocation.DataDirectory(), Folder);
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public Theme()
        {
            Settings = Settings.Instance;
            _themeDirectories.Add(DirectoryPath);
            _themeDirectories.Add(UserDirectoryPath);
            MakesureThemeDirectoriesExist();

            var dicts = Application.Current.Resources.MergedDictionaries;
            _oldResource = dicts.First(d =>
            {
                var p = d.Source.AbsolutePath;
                var dir = Path.GetDirectoryName(p).NonNull();
                var info = new DirectoryInfo(dir);
                var f = info.Name;
                var e = Path.GetExtension(p);
                var found = f == Folder && e == Extension;
                return found;
            });

            // https://github.com/Wox-launcher/Wox/issues/2935
            var support = Environment.OSVersion.Version.Major >= new Version(10, 0).Major;
            Logger.WoxInfo($"Runtime Version {Environment.OSVersion.Version} {support}");
            if (support)
            {
                AutoReload();
            }
        }

        private void MakesureThemeDirectoriesExist()
        {
            foreach (string dir in _themeDirectories)
            {
                if (!Directory.Exists(dir))
                {
                    try
                    {
                        Directory.CreateDirectory(dir);
                    }
                    catch (Exception e)
                    {
                        Logger.WoxError($"Exception when create directory <{dir}>", e);
                    }
                }
            }
        }

        public bool ChangeTheme(string theme)
        {
            const string defaultTheme = "Dark";

            string path = GetThemePath(theme);
            try
            {
                if (string.IsNullOrEmpty(path))
                    throw new DirectoryNotFoundException("Theme path can't be found <{path}>");

                Settings.Theme = theme;

                var dicts = Application.Current.Resources.MergedDictionaries;

                dicts.Remove(_oldResource);
                var newResource = GetResourceDictionary();
                dicts.Add(newResource);
                _oldResource = newResource;
                //SetBlurForWindow();
            }
            catch (DirectoryNotFoundException)
            {
                Logger.WoxError($"Theme <{theme}> path can't be found");
                if (theme != defaultTheme)
                {
                    MessageBox.Show(string.Format(InternationalizationManager.Instance.GetTranslation("theme_load_failure_path_not_exists"), theme));
                    ChangeTheme(defaultTheme);
                }
                return false;
            }
            catch (XamlParseException)
            {
                Logger.WoxError($"Theme <{theme}> fail to parse");
                if (theme != defaultTheme)
                {
                    MessageBox.Show(string.Format(InternationalizationManager.Instance.GetTranslation("theme_load_failure_parse_error"), theme));
                    ChangeTheme(defaultTheme);
                }
                return false;
            }
            return true;
        }

        public ResourceDictionary GetResourceDictionary()
        {
            var uri = GetThemePath(Settings.Theme);
            var dict = new ResourceDictionary
            {
                Source = new Uri(uri, UriKind.Absolute)
            };

            Style queryBoxStyle = dict["QueryBoxStyle"] as Style;
            if (queryBoxStyle != null)
            {
                queryBoxStyle.Setters.Add(new Setter(TextBox.FontFamilyProperty, new FontFamily(Settings.QueryBoxFont)));
                queryBoxStyle.Setters.Add(new Setter(TextBox.FontStyleProperty, FontHelper.GetFontStyleFromInvariantStringOrNormal(Settings.QueryBoxFontStyle)));
                queryBoxStyle.Setters.Add(new Setter(TextBox.FontWeightProperty, FontHelper.GetFontWeightFromInvariantStringOrNormal(Settings.QueryBoxFontWeight)));
                queryBoxStyle.Setters.Add(new Setter(TextBox.FontStretchProperty, FontHelper.GetFontStretchFromInvariantStringOrNormal(Settings.QueryBoxFontStretch)));

                var caretBrushPropertyValue = queryBoxStyle.Setters.OfType<Setter>().Any(x => x.Property == TextBox.CaretBrushProperty);
                var foregroundPropertyValue = queryBoxStyle.Setters.OfType<Setter>().FirstOrDefault(x => x.Property == TextBox.ForegroundProperty)?.Value;
                if (!caretBrushPropertyValue && foregroundPropertyValue != null)
                    queryBoxStyle.Setters.Add(new Setter(TextBox.CaretBrushProperty, foregroundPropertyValue));
            }

            var queryTextSuggestionBoxStyle = new Style(typeof(TextBox), queryBoxStyle);
            bool hasSuggestion = false;
            if (dict.Contains("QueryTextSuggestionBoxStyle"))
            {
                queryTextSuggestionBoxStyle = dict["QueryTextSuggestionBoxStyle"] as Style;
                hasSuggestion = true;
            }
            dict["QueryTextSuggestionBoxStyle"] = queryTextSuggestionBoxStyle;
            if (queryTextSuggestionBoxStyle != null)
            {
                queryTextSuggestionBoxStyle.Setters.Add(new Setter(TextBox.FontFamilyProperty, new FontFamily(Settings.QueryBoxFont)));
                queryTextSuggestionBoxStyle.Setters.Add(new Setter(TextBox.FontStyleProperty, FontHelper.GetFontStyleFromInvariantStringOrNormal(Settings.QueryBoxFontStyle)));
                queryTextSuggestionBoxStyle.Setters.Add(new Setter(TextBox.FontWeightProperty, FontHelper.GetFontWeightFromInvariantStringOrNormal(Settings.QueryBoxFontWeight)));
                queryTextSuggestionBoxStyle.Setters.Add(new Setter(TextBox.FontStretchProperty, FontHelper.GetFontStretchFromInvariantStringOrNormal(Settings.QueryBoxFontStretch)));
            }

            var queryBoxStyleSetters = queryBoxStyle.Setters.OfType<Setter>().ToList();
            var queryTextSuggestionBoxStyleSetters = queryTextSuggestionBoxStyle.Setters.OfType<Setter>().ToList();
            foreach (Setter setter in queryBoxStyleSetters)
            {
                if (setter.Property == TextBox.BackgroundProperty)
                    continue;
                if (setter.Property == TextBox.ForegroundProperty)
                    continue;
                if (queryTextSuggestionBoxStyleSetters.All(x => x.Property != setter.Property))
                    queryTextSuggestionBoxStyle.Setters.Add(setter);
            }

            if (!hasSuggestion)
            {
                var backgroundBrush = queryBoxStyle.Setters.OfType<Setter>().FirstOrDefault(x => x.Property == TextBox.BackgroundProperty)?.Value ??
                    (dict["BaseQuerySuggestionBoxStyle"] as Style).Setters.OfType<Setter>().FirstOrDefault(x => x.Property == TextBox.BackgroundProperty).Value;
                queryBoxStyle.Setters.OfType<Setter>().FirstOrDefault(x => x.Property == TextBox.BackgroundProperty).Value = Brushes.Transparent;
                if (queryTextSuggestionBoxStyle.Setters.OfType<Setter>().Any(x => x.Property == TextBox.BackgroundProperty))
                {
                    queryTextSuggestionBoxStyle.Setters.OfType<Setter>().First(x => x.Property == TextBox.BackgroundProperty).Value = backgroundBrush;
                }
                else
                {
                    queryTextSuggestionBoxStyle.Setters.Add(new Setter(TextBox.BackgroundProperty, backgroundBrush));
                }
            }

            Style resultItemStyle = dict["ItemTitleStyle"] as Style;
            Style resultSubItemStyle = dict["ItemSubTitleStyle"] as Style;
            Style resultItemSelectedStyle = dict["ItemTitleSelectedStyle"] as Style;
            Style resultSubItemSelectedStyle = dict["ItemSubTitleSelectedStyle"] as Style;
            if (resultItemStyle != null && resultSubItemStyle != null && resultSubItemSelectedStyle != null && resultItemSelectedStyle != null)
            {
                Setter fontFamily = new Setter(TextBlock.FontFamilyProperty, new FontFamily(Settings.ResultFont));
                Setter fontStyle = new Setter(TextBlock.FontStyleProperty, FontHelper.GetFontStyleFromInvariantStringOrNormal(Settings.ResultFontStyle));
                Setter fontWeight = new Setter(TextBlock.FontWeightProperty, FontHelper.GetFontWeightFromInvariantStringOrNormal(Settings.ResultFontWeight));
                Setter fontStretch = new Setter(TextBlock.FontStretchProperty, FontHelper.GetFontStretchFromInvariantStringOrNormal(Settings.ResultFontStretch));

                Setter[] setters = { fontFamily, fontStyle, fontWeight, fontStretch };
                Array.ForEach(new[] { resultItemStyle, resultSubItemStyle, resultItemSelectedStyle, resultSubItemSelectedStyle }, o => Array.ForEach(setters, p => o.Setters.Add(p)));
            }
            return dict;
        }

        public List<string> LoadAvailableThemes()
        {
            List<string> themes = new List<string>();
            foreach (var themeDirectory in _themeDirectories)
            {
                themes.AddRange(
                    Directory.GetFiles(themeDirectory)
                        .Where(filePath => filePath.EndsWith(Extension) && !filePath.EndsWith("Base.xaml"))
                        .ToList());
            }
            return themes.OrderBy(o => o).ToList();
        }

        private string GetThemePath(string themeName)
        {
            foreach (string themeDirectory in _themeDirectories)
            {
                string path = Path.Combine(themeDirectory, themeName + Extension);
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return string.Empty;
        }

        #region Automatic theme reload based on UI Accent Color Change

        private object UISettings;

        private void AutoReload()
        {

            var uiSettings = new Windows.UI.ViewManagement.UISettings();
            uiSettings.ColorValuesChanged +=
                (sender, args) =>
                {
                    Application.Current.Dispatcher.Invoke(
                        () =>
                        {
                            ChangeTheme(Settings.Theme);
                        });
                };
            UISettings = uiSettings;
        }

        #endregion

        #region Blur Handling
        /*
        Found on https://github.com/riverar/sample-win10-aeroglass
        */
        private enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_INVALID_STATE = 4
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        private enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }
        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        /// <summary>
        /// Sets the blur for a window via SetWindowCompositionAttribute
        /// </summary>
        public void SetBlurForWindow()
        {
            // Exception of FindResource can't be cathed if global exception handle is set
            if (Environment.OSVersion.Version >= new Version(6, 2))
            {

                var resource = Application.Current.TryFindResource("ThemeBlurEnabled");
                var blur = false;
                if (resource is bool b)
                    blur = b;

                var accent = blur ? AccentState.ACCENT_ENABLE_BLURBEHIND : AccentState.ACCENT_DISABLED;
                SetWindowAccent(Application.Current.MainWindow, accent);
            }
        }

        public void DisableBlur()
        {
            // Exception of FindResource can't be cathed if global exception handle is set
            if (Environment.OSVersion.Version >= new Version(6, 2))
            {
                SetWindowAccent(Application.Current.MainWindow, AccentState.ACCENT_DISABLED);
            }
        }

        private void SetWindowAccent(Window w, AccentState state)
        {
            var windowHelper = new WindowInteropHelper(w);
            var accent = new AccentPolicy { AccentState = state };
            var accentStructSize = Marshal.SizeOf(accent);

            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData
            {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };

            SetWindowCompositionAttribute(windowHelper.Handle, ref data);

            Marshal.FreeHGlobal(accentPtr);
        }
        #endregion
    }
}
