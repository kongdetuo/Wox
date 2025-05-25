using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Wox.Plugin
{
    public interface IAsyncPlugin
    {
        Task<List<IResult>> QueryAsync(Query query, CancellationToken token);

        Task InitAsync(PluginInitContext context);

        virtual IEnumerable<PluginOption> Options => [];
        virtual void SaveOptions(IEnumerable<PluginOption> options) { }
    }

    public interface IPlugin : IAsyncPlugin
    {
        List<IResult> Query(Query query);

        virtual void Init(PluginInitContext context) { }

        Task<List<IResult>> IAsyncPlugin.QueryAsync(Query query, CancellationToken token)
            => Task.Run(() => Query(query));
        Task IAsyncPlugin.InitAsync(PluginInitContext context)
            => Task.Run(() => Init(context));
    }

    public interface IUpdateablePlugin : IAsyncPlugin
    {

        IAsyncEnumerable<List<IResult>> QueryUpdateAsync(Query query, CancellationToken token);
    }
}