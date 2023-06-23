using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Wox.Core.Plugin;
using Wox.Infrastructure;
using Wox.Infrastructure.Logger;
using Wox.Plugin;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Channels;
using Wox.Infrastructure.UserSettings;
using System.Diagnostics;
using Wox.Core.Storage;
using NLog.Config;

namespace Wox.Core.Services
{
    public class QueryService
    {
        private readonly TopMostRecord topMostRecord;
        private readonly UserSelectedRecord userSelectedRecord;
        private readonly Logger Logger;

        public QueryService(TopMostRecord topMostRecord, UserSelectedRecord userSelectedRecord)
        {
            this.topMostRecord = topMostRecord;
            this.userSelectedRecord = userSelectedRecord;
            this.Logger = LogManager.GetCurrentClassLogger();

            // Query(QueryBuilder.Build("我")).IgnoreElements().Subscribe();
        }

        public IObservable<PluginQueryResult> Query(Query query)
        {
            return Observable.Create<PluginQueryResult>(async (ob, token) =>
            {
                var plugins = PluginManager.AllPlugins;
                await Task.WhenAll(plugins.AsParallel().Select(async plugin =>
                {
                    try
                    {
                        if (query.IsEmpty || !TryMatch(plugin, query))
                        {
                            ob.OnNext(Empty(plugin, query));
                            return;
                        }

                        ob.OnNext(await QueryAsync(plugin, query, token));

                        await foreach (var item in QueryUpdate(plugin, query, token))
                        {
                            if (token.IsCancellationRequested)
                                break;
                            ob.OnNext(item);
                        }
                    }
                    catch (Exception)
                    {

                    }
                }));
                ob.OnCompleted();
            });
        }

        private PluginQueryResult CreatePluginQueryResult(PluginProxy plugin, Query query, List<Result> results)
        {
            var result = new PluginQueryResult(plugin, query, results ?? new());
            UpdatePluginMetadata(result.Results, plugin.Metadata, query);
            UpdateScore(result);
            return result;
        }


        public IAsyncEnumerable<PluginQueryResult> QueryUpdate(PluginProxy plugin, Query query, CancellationToken token)
        {
            if (plugin.Plugin is IResultUpdated updatedPlugin)
            {
                return updatedPlugin.QueryUpdates(query, token).Select(p => CreatePluginQueryResult(plugin, query, p).WithToken(token));
            }
            return AsyncEnumerable.Empty<PluginQueryResult>();
        }



        public async Task<PluginQueryResult> QueryAsync(PluginProxy pair, Query query, CancellationToken token)
        {
            try
            {
                var metadata = pair.Metadata;
                List<Result> results = new();
                var milliseconds = await Logger.StopWatchDebugAsync($"Query <{query.RawQuery}> Cost for {metadata.Name}", async () =>
                {
                    results = await pair.QueryAsync(query, token) ?? new List<Result>();
                });

                metadata.QueryCount += 1;
                metadata.AvgQueryTime = metadata.QueryCount == 1 ? milliseconds : (metadata.AvgQueryTime + milliseconds) / 2;
                return CreatePluginQueryResult(pair, query, results).WithToken(token);
            }
            catch (Exception e)
            {
                e.Data.Add(nameof(pair.Metadata.ID), pair.Metadata.ID);
                e.Data.Add(nameof(pair.Metadata.Name), pair.Metadata.Name);
                e.Data.Add(nameof(pair.Metadata.PluginDirectory), pair.Metadata.PluginDirectory);
                e.Data.Add(nameof(pair.Metadata.Website), pair.Metadata.Website);
                Logger.WoxError($"Exception for plugin <{pair.Metadata.Name}> when query <{query}>", e);
                return Empty(pair, query);
            }
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
        private static Result SetPluginMetadata(Result result, PluginMetadata plugin, Query query)
        {
            // ActionKeywordAssigned is used for constructing MainViewModel's query text auto-complete suggestions
            // Plugins may have multi-actionkeywords eg. WebSearches. In this scenario it needs to be overriden on the plugin level
            if (plugin.ActionKeywords.Count == 1)
                result.ActionKeywordAssigned = query.ActionKeyword;
            return result;
        }
        private static bool TryMatch(PluginProxy pair, Query query)
        {
            if (pair.Metadata.Disabled)
                return false;

            var keyword = query.ActionKeyword ?? Keyword.Global;
            return pair.MatchKeyWord(keyword);
        }
        public static void UpdatePluginMetadata(List<Result> results, PluginMetadata metadata, Query query)
        {
            foreach (var r in results)
            {
                SetPluginMetadata(r, metadata, query);
            }
        }

        private static PluginQueryResult Empty(PluginProxy plugin, Query query)
        {
            return new PluginQueryResult(plugin, query, new List<Result>());
        }
    }

    public class PluginQueryResult
    {
        public PluginQueryResult(PluginProxy plugin, Query query, List<Result> results)
        {
            this.Plugin = plugin;
            this.PluginID = plugin.Metadata.ID;
            this.Query = query;
            this.Results = results;
        }

        public PluginQueryResult(List<Result> newRawResults, string resultId)
        {
            this.Results = newRawResults;
            this.PluginID = resultId;
        }

        public PluginProxy? Plugin { get; private set; }

        public string PluginID { get; set; }

        public Query? Query { get; private set; }

        public List<Result> Results { get; set; }

        public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

        public PluginQueryResult WithToken(CancellationToken token)
        {
            this.CancellationToken = token;
            return this;
        }
    }

    static class AsyncEnumerable
    {
        public async static IAsyncEnumerable<T> Empty<T>()
        {
            await ValueTask.CompletedTask;
            yield break;
        }

        public static async IAsyncEnumerable<R> Select<T, R>(this IAsyncEnumerable<T> source, Func<T, R> selector)
        {
            await foreach (var item in source)
            {
                yield return selector(item);
            }
        }
    }
}