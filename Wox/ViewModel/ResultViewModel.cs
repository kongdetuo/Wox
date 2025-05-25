
using Avalonia.Media;
using NLog;
using ReactiveUI;
using Wox.Core.Plugin;
using Wox.Core.Services;
using Wox.Image;
using Wox.Infrastructure;
using Wox.Infrastructure.Logger;
using Wox.Infrastructure.UserSettings;
using Wox.Plugin;

namespace Wox.ViewModel
{
    public class ResultViewModel : ReactiveUI.ReactiveObject
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private IImage image;

        public ResultViewModel(ResultWrapper result, Core.Services.PluginQueryResult? rs = null)
        {
            Result = result;
 
            if (rs != null)
            {
                this.Query = rs.Query;
                this.PluginMetadata = rs.Plugin?.Metadata;
            }

            this.Score = result.Score;
        }

        public string PluginId => this.PluginMetadata?.ID ?? "";

        public int Score { get; init; }

        private void LoadImage()
        {
            var context = new ImageLoadContext()
            {
                WoxDirectory = Constant.ProgramDirectory,
                PluginDirectory = this.Result.Plugin?.Metadata?.PluginDirectory
            };

            if(this.Result.Result is Plugin.Result r && !string.IsNullOrEmpty(r.IcoPath))
            {
                this.Image =(IImage) App.API.IconHelper.FromImage(r.IcoPath).Load(context);
            }else if(this.Result.Result.IconLoader != null)
            {
                this.Color = Colors.Transparent;
                if (this.Result.Result.IconLoader is IconHelper.ColorIconLoader c)
                {
                    this.Color = (Color)c.Load(null!);
                }
                else
                {
                    this.Image = (IImage)this.Result.Result.IconLoader?.Load(context);
                }
            }
        }


        // directly binding will cause unnecessory image load
        // only binding get will cause load twice or more
        // so use lazy binding

        private bool loaded = false;

        public IImage Image
        {
            get
            {
                if (!loaded)
                {
                    loaded = true;
                    LoadImage();
                }

                return image;
            }
            set
            {
                image = value;
                this.RaisePropertyChanged(nameof(Image));
            }
        }

        public Color Color { get; set => this.RaiseAndSetIfChanged(ref field, value); } = Colors.Transparent;

        public ResultWrapper Result { get; set; }

        public PluginMetadata? PluginMetadata { get; set; }

        public Query? Query { get; set; }
    }
}