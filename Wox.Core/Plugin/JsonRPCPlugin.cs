using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using NLog;
using Wox.Infrastructure.Exception;
using Wox.Infrastructure.Logger;
using Wox.Plugin;

namespace Wox.Core.Plugin
{
    /// <summary>
    /// Represent the plugin that using JsonPRC
    /// every JsonRPC plugin should has its own plugin instance
    /// </summary>
    internal abstract class JsonRPCPlugin : IAsyncPlugin, IAsyncContextMenu
    {
        protected PluginInitContext context;
        public const string JsonRPC = "JsonRPC";

        /// <summary>
        /// The language this JsonRPCPlugin support
        /// </summary>
        public abstract string SupportedLanguage { get; set; }

        protected abstract Task<string> ExecuteQuery(Query query);
        protected abstract Task<string> ExecuteCallback(JsonRPCRequestModel rpcRequest);
        protected abstract Task<string> ExecuteContextMenu(Result selectedResult);

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public async Task<List<Result>> QueryAsync(Query query, CancellationToken token)
        {
            string output = await ExecuteQuery(query);
            try
            {
                return DeserializedResult(output);
            }
            catch (Exception e)
            {
                Logger.WoxError($"Exception when query <{query}>", e);
                return null;
            }
        }

        public async Task<List<Result>> LoadContextMenusAsync(Result selectedResult)
        {
            string output = await ExecuteContextMenu(selectedResult);
            try
            {
                return DeserializedResult(output);
            }
            catch (Exception e)
            {
                Logger.WoxError($"Exception on result <{selectedResult}>", e);
                return null;
            }
        }

        private List<Result> DeserializedResult(string output)
        {
            if (!String.IsNullOrEmpty(output))
            {
                List<Result> results = new List<Result>();

                JsonRPCQueryResponseModel queryResponseModel = JsonConvert.DeserializeObject<JsonRPCQueryResponseModel>(output);
                if (queryResponseModel.Result == null) return null;

                foreach (JsonRPCResult result in queryResponseModel.Result)
                {
                    JsonRPCResult result1 = result;
                    result.AsyncAction = async c =>
                    {
                        if (result1.JsonRPCAction == null) return false;

                        if (!String.IsNullOrEmpty(result1.JsonRPCAction.Method))
                        {
                            if (result1.JsonRPCAction.Method.StartsWith("Wox."))
                            {
                                ExecuteWoxAPI(result1.JsonRPCAction.Method.Substring(4), result1.JsonRPCAction.Parameters);
                            }
                            else
                            {
                                string actionReponse = await ExecuteCallback(result1.JsonRPCAction);
                                JsonRPCRequestModel jsonRpcRequestModel = JsonConvert.DeserializeObject<JsonRPCRequestModel>(actionReponse);
                                if (jsonRpcRequestModel != null
                                    && !String.IsNullOrEmpty(jsonRpcRequestModel.Method)
                                    && jsonRpcRequestModel.Method.StartsWith("Wox."))
                                {
                                    ExecuteWoxAPI(jsonRpcRequestModel.Method.Substring(4), jsonRpcRequestModel.Parameters);
                                }
                            }
                        }
                        return !result1.JsonRPCAction.DontHideAfterAction;
                    };
                    results.Add(result);
                }
                return results;
            }
            else
            {
                return null;
            }
        }

        private void ExecuteWoxAPI(string method, object[] parameters)
        {
            MethodInfo methodInfo = PluginManager.API.GetType().GetMethod(method);
            if (methodInfo != null)
            {
                try
                {
                    methodInfo.Invoke(PluginManager.API, parameters);
                }
                catch (Exception)
                {
#if (DEBUG)
                    {
                        throw;
                    }
#endif
                }
            }
        }

        /// <summary>
        /// Execute external program and return the output
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        protected async Task<string> Execute(string fileName, string arguments)
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = fileName;
            start.Arguments = arguments;
            start.UseShellExecute = false;
            start.CreateNoWindow = true;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            return await Execute(start);
        }

        protected async Task<string> Execute(ProcessStartInfo startInfo)
        {
            try
            {
                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    Logger.WoxError("Can't start new process");
                    return string.Empty;
                }

                using var standardOutput = process.StandardOutput;
                var result = await standardOutput.ReadToEndAsync();
                if (!string.IsNullOrEmpty(result))
                {
                    if (!result.StartsWith("DEBUG:"))
                        return result;

                    MessageBox.Show(new Form { TopMost = true }, result.Substring(6));
                    return string.Empty;
                }

                using var standardError = process.StandardError;
                var error = await standardError.ReadToEndAsync();
                if (!string.IsNullOrEmpty(error))
                {
                    Logger.WoxError($"{error}");
                    return string.Empty;
                }
                else
                {
                    Logger.WoxError("Empty standard output and standard error.");
                    return string.Empty;
                }
            }
            catch (Exception e)
            {
                Logger.WoxError($"Exception for filename <{startInfo.FileName}> with argument <{startInfo.Arguments}>", e);
                return string.Empty;
            }
        }

        public Task InitAsync(PluginInitContext ctx)
        {
            context = ctx;
            return Task.CompletedTask;
        }
    }
}