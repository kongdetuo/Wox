using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using Wox.Infrastructure;
using Wox.Infrastructure.Storage;

namespace Wox.Plugin.WebSearch
{
    public class Main : IPlugin, ISettingProvider, IPluginI18n, ISavable, IResultUpdated
    {
        private PluginInitContext _context;

        private readonly Settings _settings;
        private readonly SettingsViewModel _viewModel;

        public const string Images = "Images";
        public static string ImagesDirectory;

        private readonly string SearchSourceGlobalPluginWildCardSign = "*";

        public Main()
        {
            _viewModel = new SettingsViewModel();
            _settings = _viewModel.Settings;
        }
        public void Save()
        {
            _viewModel.Save();
        }

        public List<Result> Query(Query query) => new();

        public async IAsyncEnumerable<List<Result>> QueryUpdates(Query query, [EnumeratorCancellation] CancellationToken token)
        {
            var enabledSource = _settings.SearchSources.Where(p => p.Enabled);
            var searchSourceList = enabledSource
                .Where(o => o.ActionKeyword == query.ActionKeyword)
                .Concat(enabledSource.Where(p => p.ActionKeyword == SearchSourceGlobalPluginWildCardSign))
                .ToList();

            if (searchSourceList.Any())
            {
                var defaultResults = searchSourceList
                    .Select(source => GetResult(source, GetKeyword(query, source), GetSubtitle(source)))
                    .ToList();

                yield return defaultResults;
                if (_settings.EnableSuggestion && _settings.SelectedSuggestion != null)
                {
                    foreach (var source in searchSourceList)
                    {
                        var suggest = await Suggestions(source, query);
                        if (!token.IsCancellationRequested)
                        {
                            // List<T> is not thread safe
                            defaultResults = defaultResults.Concat(suggest).ToList();
                            yield return defaultResults;
                        }
                    }
                }
            }
            else
            {
                yield return new List<Result>();
            }
        }

        private async Task<IEnumerable<Result>> Suggestions(SearchSource searchSource, Query query)
        {
            var source = _settings.SelectedSuggestion;
            if (source != null)
            {
                var keyword = GetKeyword(query, searchSource);
                var subtitle = GetSubtitle(searchSource);
                if (string.IsNullOrWhiteSpace(keyword))
                    return Enumerable.Empty<Result>();
                var suggestions = await source.Suggestions(keyword);
                return suggestions
                    .Select(s => GetResult(searchSource, s, subtitle));
            }
            return Enumerable.Empty<Result>();
        }

        public void Init(PluginInitContext context)
        {
            _context = context;
            var pluginDirectory = _context.CurrentPluginMetadata.PluginDirectory;
            var bundledImagesDirectory = Path.Combine(pluginDirectory, Images);
            ImagesDirectory = Path.Combine(_context.CurrentPluginMetadata.PluginDirectory, Images);
            Helper.ValidateDataDirectory(bundledImagesDirectory, ImagesDirectory);
        }

        #region ISettingProvider Members

        public Control CreateSettingPanel()
        {
            return new SettingsControl(_context, _viewModel);
        }

        #endregion

        public string GetTranslatedPluginTitle()
        {
            return _context.API.GetTranslation("wox_plugin_websearch_plugin_name");
        }

        public string GetTranslatedPluginDescription()
        {
            return _context.API.GetTranslation("wox_plugin_websearch_plugin_description");
        }



        private string GetKeyword(Query query, SearchSource searchSource)
        {
            string keyword = searchSource.ActionKeyword == SearchSourceGlobalPluginWildCardSign
                ? query.ToString()
                : query.Search;
            return keyword;
        }

        private string GetSubtitle(SearchSource searchSource)
        {
            string subtitle = _context.API.GetTranslation("wox_plugin_websearch_search") + " " + searchSource.Title;
            return subtitle;
        }

        private Result GetResult(SearchSource searchSource, string keyword, string subtitle)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                return new Result
                {
                    Score = 100,
                    Title = subtitle,
                    SubTitle = string.Empty,
                    IcoPath = searchSource.IconPath
                };
            }
            else
            {
                return new Result
                {
                    Title = keyword,
                    SubTitle = subtitle,
                    Score = 100,
                    IcoPath = searchSource.IconPath,
                    ActionKeywordAssigned = searchSource.ActionKeyword == SearchSourceGlobalPluginWildCardSign ? string.Empty : searchSource.ActionKeyword,
                    Action = c =>
                    {
                        if (_settings.OpenInNewBrowser)
                        {
                            searchSource.Url.Replace("{q}", Uri.EscapeDataString(keyword)).NewBrowserWindow(_settings.BrowserPath);
                        }
                        else
                        {
                            searchSource.Url.Replace("{q}", Uri.EscapeDataString(keyword)).NewTabInBrowser(_settings.BrowserPath);
                        }
                        return true;
                    }
                };
            }
        }

    }
}
