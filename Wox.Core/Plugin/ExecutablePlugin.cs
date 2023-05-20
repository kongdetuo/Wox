using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Wox.Plugin;

namespace Wox.Core.Plugin
{
    internal class ExecutablePlugin : JsonRPCPlugin
    {
        private readonly ProcessStartInfo _startInfo;
        public override string SupportedLanguage { get; set; } = AllowedLanguage.Executable;

        public ExecutablePlugin(string filename)
        {
            _startInfo = new ProcessStartInfo
            {
                FileName = filename,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
        }

        protected override async Task<string> ExecuteQuery(Query query)
        {
            JsonRPCServerRequestModel request = new JsonRPCServerRequestModel
            {
                Method = "query",
                Parameters = new object[] { query.Search },
            };

            _startInfo.Arguments = $"\"{request}\"";

            return await Execute(_startInfo);
        }

        protected override async Task<string> ExecuteCallback(JsonRPCRequestModel rpcRequest)
        {
            _startInfo.Arguments = $"\"{rpcRequest}\"";
            return await Execute(_startInfo);
        }

        protected override async Task< string> ExecuteContextMenu(Result selectedResult) {
            JsonRPCServerRequestModel request = new JsonRPCServerRequestModel {
                Method = "contextmenu",
                Parameters = new object[] { selectedResult.ContextData },
            };

            _startInfo.Arguments = $"\"{request}\"";

            return await Execute(_startInfo);
        }
    }
}