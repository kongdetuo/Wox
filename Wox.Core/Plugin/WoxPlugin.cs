using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wox.Infrastructure.UserSettings;
using Wox.Plugin;

namespace Wox.Core.Plugin
{
    public class WoxPlugin
    {
        public required IAsyncPlugin Instance { get; init; }
        public PluginMetadata Metadata { get; set; }

        public object Icon { get; set; }

        public bool MatchKeyWord(string word)
        {
            return Metadata.ActionKeywords.Any(p => p.Key == word);
        }

        public bool MatchKeyWord(Keyword keyword)
        {
            return Metadata.ActionKeywords.Contains(keyword);
        }

        public async IAsyncEnumerable<List<IResult>> QueryAsync(Query query, CancellationToken token)
        {
            if (query.IsEmpty || Metadata.Disabled || !MatchKeyWord(query.ActionKeyword))
            {
                yield return new List<IResult>();
                yield break;
            }

            yield return await Instance.QueryAsync(query, token);

            if(Instance is IUpdateablePlugin updateable)
            {
                await foreach (var item in updateable.QueryUpdateAsync(query, token))
                {
                    yield return item;
                }
            }
        }

        public async Task InitAsync(PluginInitContext context)
        {
            await Instance.InitAsync(context);
        }
    }
}