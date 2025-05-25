using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace Wox.Plugin.Color
{
    public sealed class ColorsPlugin : IPlugin, IPluginI18n
    {
        private PluginInitContext context = null!;

        public List<IResult> Query(Query query)
        {
            var raw = query.Search;
            if (!IsAvailable(raw)) 
                return new List<IResult>(0);
            try
            {
                return [new Result(context, raw)];
            }
            catch (Exception)
            {
                return new List<IResult>(0);
            }
        }

        private bool IsAvailable(string query)
        {
            // todo: rgb, names
            var length = query.Length - 1; // minus `#` sign
            return query.StartsWith("#") && (length == 3 || length == 6);
        }

        public void Init(PluginInitContext context)
        {
            this.context = context;
        }

        public string GetTranslatedPluginTitle()
        {
            return context.API.GetTranslation("wox_plugin_color_plugin_name");
        }

        public string GetTranslatedPluginDescription()
        {
            return context.API.GetTranslation("wox_plugin_color_plugin_description");
        }

        class Result(PluginInitContext context, String c) : IResult
        {
            public string Title { get; } = c.ToUpper();

            public string? SubTitle { get; } = "";

            public int Score { get; }

            public IconLoader? IconLoader { get; } = context.API.IconHelper.FromColor(ColorTranslator.FromHtml(c));

            public async Task<bool> InvokeAsync(ActionContext context)
            {
                await context.API.Clipboard.SetTextAsync(Title);
                return true;
            }
        }
    }
}