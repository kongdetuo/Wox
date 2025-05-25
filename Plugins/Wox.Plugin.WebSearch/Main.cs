using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wox.Infrastructure;

namespace Wox.Plugin.WebSearch
{
    public class Main : IPlugin, IPluginI18n, IUpdateablePlugin
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

        public List<IResult> Query(Query query)
        {
            var enabledSource = _settings.SearchSources.Where(p => p.Enabled);
            var source = enabledSource
                .Where(o => o.ActionKeyword == query.ActionKeyword.Key)
                .Concat(enabledSource.Where(p => p.ActionKeyword == SearchSourceGlobalPluginWildCardSign))
                .FirstOrDefault();

            if (source is not null)
            {
                return new List<IResult>()
                {
                    GetResult(source, GetKeyword(query, source), GetSubtitle(source))
                };
            }
            return null;
        }

        public async IAsyncEnumerable<List<IResult>> QueryUpdateAsync(Query query, CancellationToken token)
        {
            var enabledSource = _settings.SearchSources.Where(p => p.Enabled);
            var source = enabledSource
                .Where(o => o.ActionKeyword == query.ActionKeyword.Key)
                .Concat(enabledSource.Where(p => p.ActionKeyword == SearchSourceGlobalPluginWildCardSign))
                .FirstOrDefault();

            if (source is not null)
            {
                var defaultResults = new List<IResult>()
                {
                    GetResult(source, GetKeyword(query, source), GetSubtitle(source))
                };

                if (token.IsCancellationRequested)
                    yield break;

                if (_settings.EnableSuggestion && _settings.SelectedSuggestion != null)
                {
                    var suggest = await Suggestions(source, query);

                    // List<T> is not thread safe
                    defaultResults = defaultResults.Concat(suggest).ToList();
                  token.ThrowIfCancellationRequested();
                    yield return defaultResults;
                }
            }
            else
            {
                yield return new List<IResult>();
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
                    ActionKeywordAssigned = searchSource.ActionKeyword == SearchSourceGlobalPluginWildCardSign ? null : new Keyword(searchSource.ActionKeyword),
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

        public IEnumerable<PluginOption> Options =>
            // 这个挺复杂的，先删掉吧，到时候让他自己写配置文件
            [];

        public void SaveOptions(IEnumerable<PluginOption> options)
        {
            Save();
        }
    }
}
