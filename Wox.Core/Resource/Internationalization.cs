using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Platform;
using NLog;
using SkiaSharp;
using Wox.Core.Plugin;
using Wox.Infrastructure;
using Wox.Infrastructure.Logger;
using Wox.Infrastructure.UserSettings;
using Wox.Plugin;

namespace Wox.Core.Resource
{
    public class Internationalization
    {
        public Settings Settings { get; set; }
        private const string Folder = "Languages";
        private const string DefaultFile = "en.axaml";
        private const string Extension = ".axaml";
        private readonly List<string> _languageDirectories = new List<string>();
        private readonly List<ResourceDictionary> _oldResources = new();

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public Internationalization()
        {

            Settings = Settings.Instance;
            AddPluginLanguageDirectories();
            LoadDefaultLanguage();
            // we don't want to load /Languages/en.axaml twice
            // so add wox language directory after load plugin language files
            AddWoxLanguageDirectory();
        }


        private void AddWoxLanguageDirectory()
        {
            var directory = Path.Combine(Constant.ProgramDirectory, Folder);
            _languageDirectories.Add(directory);
        }


        private void AddPluginLanguageDirectories()
        {
            foreach (var plugin in PluginManager.GetPluginsForInterface<IPluginI18n>())
            {
                var location = Assembly.GetAssembly(plugin.Plugin.GetType()).Location;
                Logger.WoxDebug($"Plugin language location {plugin.Plugin}: {location}");
                var dir = Path.GetDirectoryName(location);
                if (dir != null)
                {
                    var pluginThemeDirectory = Path.Combine(dir, Folder);
                    _languageDirectories.Add(pluginThemeDirectory);
                }
                else
                {
                    Logger.WoxError($"Can't find plugin path <{location}> for <{plugin.Metadata.Name}>");
                }
            }
        }

        private void LoadDefaultLanguage()
        {
            LoadLanguage(AvailableLanguages.English);
            _oldResources.Clear();
        }

        public void ChangeLanguage(string languageCode)
        {
            languageCode = languageCode.NonNull();
            Language language = GetLanguageByLanguageCode(languageCode);
            ChangeLanguage(language);
        }

        public Language GetLanguageByLanguageCode(string languageCode)
        {
            var lowercase = languageCode.ToLower();
            var language = AvailableLanguages.GetAvailableLanguages().FirstOrDefault(o => o.LanguageCode.ToLower() == lowercase);
            if (language == null)
            {
                Logger.WoxError($"Language code can't be found <{languageCode}>");
                return AvailableLanguages.English;
            }
            else
            {
                return language;
            }
        }

        public void ChangeLanguage(Language language)
        {
            language = language.NonNull();

            Settings.Language = language.LanguageCode;

            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(language.LanguageCode);
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(language.LanguageCode);

            RemoveOldLanguageFiles();
            if (language != AvailableLanguages.English)
            {
                LoadLanguage(language);
            }
            UpdatePluginMetadataTranslations();

        }

        public bool PromptShouldUsePinyin(string languageCodeToSet)
        {
            var languageToSet = GetLanguageByLanguageCode(languageCodeToSet);

            if (Settings.ShouldUsePinyin)
                return false;

            if (languageToSet != AvailableLanguages.Chinese && languageToSet != AvailableLanguages.Chinese_TW)
                return false;

            //if (MessageBox.Show("Do you want to turn on search with Pinyin?", string.Empty, MessageBoxButton.YesNo) == MessageBoxResult.No)
            //    return false;

            return true;
        }

        private void RemoveOldLanguageFiles()
        {
            var dicts = Application.Current.Resources.MergedDictionaries;
            foreach (var r in _oldResources)
            {
                dicts.Remove(r);
            }
        }

        private void LoadLanguage(Language language)
        {

            var dicts = Application.Current.Resources.MergedDictionaries;
            var filename = $"{language.LanguageCode}{Extension}";
            var files = _languageDirectories
                .Select(d => LanguageFile(d, filename))
                .Where(f => !string.IsNullOrEmpty(f))
                .ToArray();


            foreach (var f in files)
            {
               // Application.Current.Resources.MergedDictionaries


                var r = (ResourceDictionary)AvaloniaRuntimeXamlLoader.Load(File.ReadAllText(f));

                dicts.Add(r);
                _oldResources.Add(r);
            }

        }

        public List<Language> LoadAvailableLanguages()
        {
            return AvailableLanguages.GetAvailableLanguages();
        }

        public string GetTranslation(string key)
        {
            if (Application.Current.TryFindResource(key, out var val) && val is string s)
            {
                return s;
            }
            else
            {
                Logger.WoxError($"No Translation for key {key}");
                return $"No Translation for key {key}";
            }
        }

        private void UpdatePluginMetadataTranslations()
        {
            foreach (var p in PluginManager.GetPluginsForInterface<IPluginI18n>())
            {
                var pluginI18N = p.Plugin as IPluginI18n;
                if (pluginI18N == null) return;
                try
                {
                    p.Metadata.Name = pluginI18N.GetTranslatedPluginTitle();
                    p.Metadata.Description = pluginI18N.GetTranslatedPluginDescription();
                }
                catch (Exception e)
                {
                    Logger.WoxError($"Failed for <{p.Metadata.Name}>", e);
                }
            }
        }

        public string LanguageFile(string folder, string language)
        {
            if (Directory.Exists(folder))
            {
                string path = Path.Combine(folder, language);
                if (File.Exists(path))
                {
                    return path;
                }
                else
                {
                    Logger.WoxError($"Language path can't be found <{path}>");
                    string english = Path.Combine(folder, DefaultFile);
                    if (File.Exists(english))
                    {
                        return english;
                    }
                    else
                    {
                        Logger.WoxError($"Default English Language path can't be found <{path}>");
                        return string.Empty;
                    }
                }
            }
            else
            {
                return string.Empty;
            }
        }
    }
}