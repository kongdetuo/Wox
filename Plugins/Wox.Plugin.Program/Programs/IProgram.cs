using System.Collections.Generic;

namespace Wox.Plugin.Program.Programs
{
    public interface IProgram
    {
        List<Result> ContextMenus(IPublicAPI api);
        IResult Result(string query, IPublicAPI api);
        string Name { get; }
        string Location { get; }
        bool Enabled { get;  }
    }
}
