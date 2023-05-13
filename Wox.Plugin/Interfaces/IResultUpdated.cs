using System.Collections.Generic;
using System.Threading;

namespace Wox.Plugin
{
    public interface IResultUpdated : IFeatures
    {
        IAsyncEnumerable<List<Result>> QueryUpdates(Query query, CancellationToken token);
    }
}
