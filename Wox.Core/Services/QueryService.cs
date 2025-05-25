using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wox.Infrastructure;
using Wox.Infrastructure.Logger;
using Wox.Plugin;
using System.Reactive.Linq;
using System.Threading;
using Wox.Core.Storage;
using Wox.Core.Resource;
using Wox.Core.Plugin;

namespace Wox.Core.Services
{
    public class QueryService(TopMostRecord topMostRecord, UserSelectedRecord userSelectedRecord, IPublicAPI API, History history)
    {
        private readonly TopMostRecord topMostRecord = topMostRecord;
        private readonly UserSelectedRecord userSelectedRecord = userSelectedRecord;
        private readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public IObservable<PluginQueryResult> Query(WoxPlugin plugin, Query query)
        {
            return Observable.Create<PluginQueryResult>(async (ob, token) =>
            {
                try
                {
                    await foreach (var item in plugin.QueryAsync(query, token))
                        ob.OnNext(CreatePluginQueryResult(plugin, query, item));
                }
                catch (Exception)
                {

                }
                ob.OnCompleted();
            });
        }

        private PluginQueryResult CreatePluginQueryResult(WoxPlugin plugin, Query query, List<IResult> results)
        {
            var result = new PluginQueryResult(plugin, query, results ?? new());
            UpdatePluginMetadata(result.Results, plugin.Metadata, query);
            UpdateScore(result);
            return result;
        }

        public async Task<PluginQueryResult> QueryContextMenuAsync(ResultWrapper result, Query query)
        {
            List<IResult> list = new List<IResult>();
            var plugin = PluginManager.GetPluginForId(result.Plugin.Metadata.ID)!;
            var metadata = plugin.Metadata;
            var translator = InternationalizationManager.Instance;

            if (plugin != null)
            {

                try
                {
                    var actionContext = new ActionContext
                    {
                        SpecialKeyState = new(),
                        API = API
                    };

                    List<IResult> results = result.Result.LoadContextMenu(actionContext);

                    list = results;
                }
                catch (Exception e)
                {
                    Logger.WoxError($"Can't load context menus for plugin <{metadata.Name}>", e);
                }
            }

            if (topMostRecord.IsTopMost(query!, metadata.ID, result))
            {
                list.Add(new Result
                {
                    Title = translator.GetTranslation("cancelTopMostInThisQuery"),
                    IcoPath = "Images\\down.png",
                    Action = _ =>
                    {
                        topMostRecord.Remove(query!);
                        API.ShowMsg("Success");
                        return false;
                    }
                });
            }
            else
            {
                list.Add(new Result
                {
                    Title = translator.GetTranslation("setAsTopMostInThisQuery"),
                    IcoPath = "Images\\up.png",
                    Action = _ =>
                    {
                        topMostRecord.AddOrUpdate(query!, metadata.ID, result);
                        API.ShowMsg("Success");
                        return false;
                    }
                });
            }



            var author = translator.GetTranslation("author");
            var website = translator.GetTranslation("website");
            var version = translator.GetTranslation("version");
            var pluginName = translator.GetTranslation("plugin");
            var title = $"{pluginName}: {metadata.Name}";
            var icon = metadata.IcoPath;
            var subtitle = $"{author}: {metadata.Author}, {website}: {metadata.Website} {version}: {metadata.Version}";

            list.Add(new Result
            {
                Title = title,
                IcoPath = icon,
                SubTitle = subtitle,
                Action = _ => false
            });

            return new PluginQueryResult(list, "Context Menu");
        }

 

        public PluginQueryResult QueryHistory(string query)
        {
            const string id = "Query History ID";
            var translator = InternationalizationManager.Instance;

            var results = new List<IResult>();
            IEnumerable<HistoryItem> items = history.Items;

            foreach (var h in history.Items.OrderByDescending(p => p.ExecutedDateTime))
            {
                var title = translator!.GetTranslation("executeQuery");
                var time = translator.GetTranslation("lastExecuteTime");
                var result = new Result
                {
                    Title = string.Format(title, h.Query),
                    SubTitle = string.Format(time, h.ExecutedDateTime),
                    IcoPath = "Images\\history.png",
                    Action = _ =>
                    {
                        API.ChangeQuery(h.Query);
                        return false;
                    }
                };
                results.Add(result);
            }

            if (!string.IsNullOrEmpty(query))
                results = results.Where(r => MatchResult(r, query)).ToList();

            return new PluginQueryResult(results, id);
        }

        private void UpdateScore(PluginQueryResult update)
        {
            var queryHasTopMoustRecord = topMostRecord.HasTopMost(update.Query!);
            foreach (var result in update.Results)
            {
                if (queryHasTopMoustRecord && topMostRecord.IsTopMost(update.Query!, update.PluginID, result))
                {
                    result.Score = int.MaxValue;
                }
                else if (!update.Plugin!.Metadata.KeepResultRawScore)
                {
                    result.Score += userSelectedRecord.GetSelectedCount(result) * 10;
                }
                else
                {
                    result.Score = result.Score;
                }
            }
        }
        private static ResultWrapper SetPluginMetadata(ResultWrapper result, PluginMetadata plugin, Query query)
        {
            // ActionKeywordAssigned is used for constructing MainViewModel's query text auto-complete suggestions
            // Plugins may have multi-actionkeywords eg. WebSearches. In this scenario it needs to be overriden on the plugin level
            if (plugin.ActionKeywords.Count == 1)
                result.ActionKeywordAssigned = query.ActionKeyword;
            return result;
        }

        public static void UpdatePluginMetadata(List<ResultWrapper> results, PluginMetadata metadata, Query query)
        {
            foreach (var r in results)
            {
                SetPluginMetadata(r, metadata, query);
            }
        }

        private static bool MatchResult(IResult result, string query)
        {
            return StringMatcher.FuzzySearch(query, result.Title).IsSearchPrecisionScoreMet()
                || StringMatcher.FuzzySearch(query, result.SubTitle ?? "").IsSearchPrecisionScoreMet();
        }
    }

    public class PluginService(IPublicAPI api)
    {


    }

    public class PluginQueryResult
    {
        public PluginQueryResult(WoxPlugin plugin, Query query, List<IResult> results)
        {
            this.Plugin = plugin;
            this.PluginID = plugin.Metadata.ID;
            this.Query = query;
            this.Results = results.Select(p => new ResultWrapper(p, plugin)).ToList();
        }

        public PluginQueryResult(List<IResult> newRawResults, string resultId)
        {
            this.Results = newRawResults.Select(p => new ResultWrapper(p, null)).ToList();
            this.PluginID = resultId;
        }

        public WoxPlugin? Plugin { get; private set; }

        public string PluginID { get; set; }

        public Query? Query { get; private set; }

        public List<ResultWrapper> Results { get; set; }

        public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

        public PluginQueryResult WithToken(CancellationToken token)
        {
            this.CancellationToken = token;
            return this;
        }
    }

    public class ResultWrapper
    {
        public ResultWrapper(IResult result, WoxPlugin metadata)
        {
            this.Result = result;
            this.Score = result.Score;
            this.Plugin = metadata;
        }
        public IResult Result { get; set; }
        public int Score { get; internal set; }
        public WoxPlugin Plugin { get; }
        public Keyword? ActionKeywordAssigned { get; internal set; }
        public string Title => Result.Title;
        public string SubTitle => Result.SubTitle ?? "";

        public override string ToString()
        {
            return Result.Title + Result.SubTitle;
        }

    }
}