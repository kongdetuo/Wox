using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Wox.Plugin
{
    public interface IPlugin : IAsyncPlugin
    {
        List<Result> Query(Query query);
        void Init(PluginInitContext context);

        Task<List<Result>> IAsyncPlugin.QueryAsync(Query query, CancellationToken token)
            => Task.Run(() => Query(query));
        Task IAsyncPlugin.InitAsync(PluginInitContext context) 
            => Task.Run(() => Init(context));
    }
}