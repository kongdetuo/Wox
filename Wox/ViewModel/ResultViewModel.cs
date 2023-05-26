using System;
using System.IO;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;
using NLog;
using Wox.Core.Plugin;
using Wox.Image;
using Wox.Infrastructure;
using Wox.Infrastructure.Logger;
using Wox.Infrastructure.UserSettings;
using Wox.Plugin;

namespace Wox.ViewModel
{
    public class ResultViewModel : BaseModel
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public ResultViewModel(Result result, Core.Services.PluginQueryResult? rs = null)
        {
            Result = result;
            Image = new Lazy<ImageSource?>(() =>
            {
                return SetImage(result);
            });

            if (rs != null)
            {
                this.Query = rs.Query;   
                this.PluginMetadata = rs.Plugin?.Metadata;
            }
        }

        public string? PluginId => this.PluginMetadata?.ID;

        public int Score => this.Result.Score;

        private ImageSource? SetImage(Result result)
        {
            if (result.IcoPath == null && this.PluginMetadata is null)
            {
                return null;
            }

            string? imagePath = result.IcoPath;

            var plugin = PluginManager.GetPluginForId(PluginMetadata?.ID);
            var pluginDirectory = plugin?.Metadata?.PluginDirectory;
            try
            {
                // will get here either when icoPath has value\icon delegate is null\when had exception in delegate
                return ImageLoader.Load(imagePath, UpdateImageCallback, result.Title.Text, PluginId, pluginDirectory);
            }
            catch (Exception e)
            {
                e.Data.Add(nameof(result.Title), result.Title);
                e.Data.Add(nameof(PluginId), PluginId);
                e.Data.Add(nameof(result.IcoPath), result.IcoPath);
                Logger.WoxError($"Cannot read image {result.IcoPath}", e);
                return ImageLoader.GetErrorImage();
            }
        }

        public void UpdateImageCallback(ImageSource image)
        {
            Image = new Lazy<ImageSource?>(() => image);
            OnPropertyChanged(nameof(Image));
        }

        // directly binding will cause unnecessory image load
        // only binding get will cause load twice or more
        // so use lazy binding
        public Lazy<ImageSource?> Image { get; set; }

        public Result Result { get; set; }

        public PluginMetadata? PluginMetadata { get; set; }

        public Query? Query { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj is ResultViewModel r)
            {
                return Result.Equals(r.Result);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return Result.GetHashCode();
        }

        public override string ToString()
        {
            return Result.ToString();
        }
    }
}