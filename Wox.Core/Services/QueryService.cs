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

        public static IObservable<PluginQueryResult> Query(Query query, System.Threading.CancellationToken token)
        {
            var plugins = PluginManager.AllPlugins;//.Where(p => p.Metadata.Name.Contains("程序"));
            return plugins.ToObservable()
                .ObserveOn(ThreadPoolScheduler.Instance)
                .SelectMany(plugin => Observable.FromAsync(() => QueryForPluginAsync(plugin, query, token)));
        }

        public static async Task<PluginQueryResult> QueryForPluginAsync(PluginPair pair, Query query, System.Threading.CancellationToken t)
        {
            if (query == null || t.IsCancellationRequested || pair.Metadata.Disabled)
            {
                return new PluginQueryResult()
                {
                    PluginID = pair.Metadata.ID,
                    Query = query,
                    Results = new List<Result>()
                };
            }
            await Task.Yield();
            var results = PluginManager.QueryForPlugin(pair, query);
            return new PluginQueryResult()
            {
                PluginID = pair.Metadata.ID,
                Query = query,
                Results = results,
            };
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
        public string PluginID { get; set; }

        public Query Query { get; set; }

        public List<Result> Results { get; set; }
    }
}
