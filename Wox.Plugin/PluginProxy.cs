using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Wox.Plugin
{
    public class PluginProxy
    {
        public IAsyncPlugin Plugin { get;  set; }
        public PluginMetadata Metadata { get;  set; }
        public bool MatchKeyWord(string word)
        {
            return this.Metadata.ActionKeywords.Any(p => p.Key == word);
        }

        public bool MatchKeyWord(Keyword keyword)
        {
            return this.Metadata.ActionKeywords.Contains(keyword);
        }
        
        public async Task<List<Result>> QueryAsync(Query query, CancellationToken token)
        {
            return await Plugin.QueryAsync(query, token);
        }

        public async Task InitAsync(PluginInitContext context)
        {
            await Plugin.InitAsync(context);
        }

        public IAsyncEnumerable<List<Result>> QueryUpdates(Query query, CancellationToken token)
        {
            if(Plugin is IResultUpdated updated)
            {
                return updated.QueryUpdates(query, token);
            }
            return null;
        }
    }
}
