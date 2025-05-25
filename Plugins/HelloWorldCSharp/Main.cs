using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wox.Plugin;

namespace HelloWorldCSharp
{
    class Main : IPlugin
    {
        private PluginInitContext context;

        public List<IResult> Query(Query query)
        {
            System.Reactive.Linq.Observable.Return(1);
            var result = new Result1
            {
                Title = "Hello World from CSharp",
                SubTitle = $"Query: {query.Search}",
                IconLoader = context.API.IconHelper.FromImage(Path.Combine("Images", "app.png"))
            };
            return new List<IResult> { result };
        }

        public void Init(PluginInitContext context)
        {
            this.context = context;
        }
}
}
