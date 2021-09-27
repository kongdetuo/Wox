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

namespace Wox.Core.Services
{
    public class QueryService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static IObservable<PluginQueryResult> Query(Query query)
        {
            var plugins = PluginManager.AllPlugins;
            if (query == null)
                return plugins.Select(p => new PluginQueryResult(p, query, new List<Result>())).ToObservable();

            return plugins.ToObservable()
                .ObserveOn(ThreadPoolScheduler.Instance)
                .SelectMany(plugin => QueryPluginAsync(plugin, query).ToObservable());
        }

        private static async IAsyncEnumerable<PluginQueryResult> QueryPluginAsync(PluginPair pair, Query query)
        {
            if (pair.Metadata.Disabled  || !TryMatch(pair, query))
            {
                yield return new PluginQueryResult(pair, query, new List<Result>());
                yield break;
            }

            await Task.Yield();
            var firstResult = new PluginQueryResult(pair, query, null);
            firstResult.Results = PluginManager.QueryForPlugin(pair, query);

            yield return firstResult;

            if (pair.Plugin is IResultUpdated updatedPlugin)
            {
                await foreach (var results in updatedPlugin.QueryUpdates(query))
                {
                    PluginManager.UpdatePluginMetadata(results, pair.Metadata, query);
                    yield return new PluginQueryResult(pair, query, results);
                }
            }
        }

        private static bool TryMatch(PluginPair pair, Query query)
        {
            bool validGlobalQuery = string.IsNullOrEmpty(query.ActionKeyword) && pair.Metadata.ActionKeywords[0] == Wox.Plugin.Query.GlobalPluginWildcardSign;
            bool validNonGlobalQuery = pair.Metadata.ActionKeywords.Contains(query.ActionKeyword);
            return validGlobalQuery || validNonGlobalQuery;
        }
    }

    public class PluginQueryResult
    {
        public PluginQueryResult(PluginPair plugin, Query query, List<Result> results)
        {
            this.PluginID = plugin.Metadata.ID;
            this.Query = query;
            this.Results = results;
        }
        public string PluginID { get; set; }

        public Query Query { get; set; }

        public List<Result> Results { get; set; }
    }
}
