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

namespace Wox.Core.Services
{
    public class QueryService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static IAsyncEnumerable<PluginQueryResult> QueryAsync(Query query, CancellationToken token)
        {
            var channel = Channel.CreateUnbounded<PluginQueryResult>();
            _ = Task.Run(async () =>
            {
                var plugins = PluginManager.AllPlugins;
                var writer = channel.Writer;
                var tasks = plugins.AsParallel().Select(async plugin =>
                {
                    if (query is null || !TryMatch(plugin, query))
                    {
                        await channel.Writer.WriteAsync(Empty(plugin, query), token);
                    }
                    else
                    {
                        await writer.WriteAsync(new PluginQueryResult(plugin, query, PluginManager.QueryForPlugin(plugin, query)), token);
                        if (plugin.Plugin is IResultUpdated updatedPlugin)
                        {
                            await foreach (var item in updatedPlugin.QueryUpdates(query).WithCancellation(token))
                            {
                                PluginManager.UpdatePluginMetadata(item, plugin.Metadata, query);
                                await writer.WriteAsync(new PluginQueryResult(plugin, query, item), token);
                            }
                        }
                    }
                });

                await Task.WhenAll(tasks);
                writer.Complete();
            }, CancellationToken.None);
            return channel.Reader.ReadAllAsync(token);
        }

        private static bool TryMatch(PluginProxy pair, Query query)
        {
            if (pair.Metadata.Disabled)
                return false;

            bool validGlobalQuery = query.ActionKeyword is null && pair.Metadata.ActionKeywords[0].IsGlobal;
            bool validNonGlobalQuery = query.ActionKeyword is not null && pair.Metadata.ActionKeywords.Contains(query.ActionKeyword.Value);
            return validGlobalQuery || validNonGlobalQuery;
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

        public PluginProxy Plugin { get; private set; }

        public string PluginID { get; set; }

        public Query Query { get; private set; }

        public List<Result> Results { get; set; }
    }
}