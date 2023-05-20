using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Wox.Plugin
{
    public interface IFeatures { }

    public interface IContextMenu : IFeatures
    {
        List<Result> LoadContextMenus(Result selectedResult);
    }

    public interface IAsyncContextMenu : IFeatures
    {
        Task<List<Result>> LoadContextMenusAsync(Result selectedResult);
    }

    /// <summary>
    /// Represent plugins that support internationalization
    /// </summary>
    public interface IPluginI18n : IFeatures
    {
        string GetTranslatedPluginTitle();

        string GetTranslatedPluginDescription();
    }
}
