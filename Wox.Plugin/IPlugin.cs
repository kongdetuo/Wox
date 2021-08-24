using System.Collections.Generic;
using System.Threading.Tasks;

namespace Wox.Plugin
{
    public interface IPlugin
    {
        List<Result> Query(Query query);
        void Init(PluginInitContext context);
    }

    public interface IAsyncPlugin : IPlugin
    {
        Task<List<Result>> QueryAsync(Query query);
    }
}