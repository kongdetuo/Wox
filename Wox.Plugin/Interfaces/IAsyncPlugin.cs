using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Wox.Plugin
{
    public interface IAsyncPlugin
    {
        Task<List<Result>> QueryAsync(Query query, CancellationToken token);

        virtual Task InitAsync(PluginInitContext context)
        {
            return Task.CompletedTask;
        }
    }
}