using System;
using System.IO;
using System.Linq;
using Microsoft.WindowsAPICodePack.Shell;
using NLog;
using Wox.Infrastructure;
using Wox.Infrastructure.Logger;
using Avalonia.Media.Imaging;
using System.Windows.Media.Imaging;

namespace Wox.Image
{
    public static class ImageLoader
    {
        private static readonly string[] ImageExtensions =
        {
            ".png",
            ".jpg",
            ".jpeg",
            ".gif",
            ".bmp",
            ".tiff",
            ".ico"
        };

        private static ImageCache _cache = null!;

        private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();
        private static Bitmap _defaultFileImage = null!;
        private static Bitmap _errorImage = null!;

        public static void Initialize()
        {

            string defaultFilePath = Path.Combine(Constant.ImagesDirectory, "file.png");
            _defaultFileImage = new Bitmap(defaultFilePath).CreateScaledBitmap(new Avalonia.PixelSize(32, 32));


            _errorImage = new Bitmap(Constant.ErrorIcon).CreateScaledBitmap(new Avalonia.PixelSize(32, 32));

            _cache = new ImageCache();
        }

        private static Bitmap LoadInternal(string? path, string? pluginDirectory)
        {
            Logger.WoxDebug($"load from disk {path}");
            if (string.IsNullOrEmpty(path))
            {
                return _defaultFileImage;
            }

            var image = LoadEmbededImage(path)
                ?? LoadDataImage(path)
                ?? LoadNormalImage(path, pluginDirectory)
                ?? LoadDirectoryThumbnail(path)
                ?? LoadDirectoryThumbnail(path)
                ?? LoadFileThumbnail(path)
                ?? LoadFileThumbnail(path) // sometimes first try will throw exception, but second try will be ok.
                ?? GetErrorImage();

            return image;
        }

        private static Bitmap? LoadFileThumbnail(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    // https://stackoverflow.com/a/1751610/2833083
                    // https://stackoverflow.com/questions/21751747/extract-thumbnail-for-any-file-in-windows
                    // https://docs.microsoft.com/en-us/windows/win32/api/shobjidl_core/nf-shobjidl_core-ishellitemimagefactory-getimage
                    using ShellFile shell = ShellFile.FromFilePath(path);
                    // https://github.com/aybe/Windows-API-Code-Pack-1.1/blob/master/source/WindowsAPICodePack/Shell/Common/ShellThumbnail.cs#L333
                    // https://github.com/aybe/Windows-API-Code-Pack-1.1/blob/master/source/WindowsAPICodePack/Shell/Common/DefaultShellImageSizes.cs#L46
                    // small is (32, 32)

                    return shell.Thumbnail.SmallBitmapSource.ToAvaloniaBitmap();
                }
                catch (ShellException e)
                {
                    _ = e;
                }
            }
            return null;
        }

        private static Bitmap? LoadDirectoryThumbnail(string? path)
        {
            if (string.IsNullOrEmpty(path) == false && Directory.Exists(path))
            {

                try
                {
                    while (path.Contains(@"\\"))
                    {
                        path = path.Replace("\\\\", "\\");
                    }

                    // can be extended to support guid things
                    using ShellObject shell = ShellFile.FromParsingName(path);

                    return shell.Thumbnail.SmallBitmapSource.ToAvaloniaBitmap();
                }
                catch (Exception e)
                {
                    e.Data.Add(nameof(path), path);
                    Logger.WoxError($"cannot load {path}", e);
                    return GetErrorImage();
                }
            }
            return null;
        }

        public static Bitmap? ToAvaloniaBitmap( this System.Windows.Media.ImageSource bitmap)
        {
            if (bitmap == null)
                return null;
            var a = (BitmapSource)bitmap;
            var b = new PngBitmapEncoder();
            b.Frames.Add( BitmapFrame.Create(a));
            using var mem = new MemoryStream();
            b.Save(mem);
            mem.Seek(0, SeekOrigin.Begin);
            return new Bitmap(mem);
        }

        private static Bitmap? LoadNormalImage(string? path, string? pluginDirectory)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            bool normalImage = ImageExtensions.Any(e => path.EndsWith(e));

            if (normalImage)
            {
                return TryLoadFromPath(path)
                    ?? TryLoadFromPluginDirectory(path, pluginDirectory)
                    ?? TryLoadFromWoxDirectory(path);
            }
            return null;
        }

        private static Bitmap? LoadEmbededImage(string path)
        {
            try
            {
                string key = "EmbededIcon:";
                if (path.StartsWith(key))
                {
                    return EmbededIcon.GetImage(key, path, 32);
                }
            }
            catch (Exception)
            {
            }

            return null;
        }

        private static Bitmap? LoadDataImage(string path)
        {
            if (path.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    //BitmapImage image = new();
                    //image.BeginInit();
                    //image.CacheOption = BitmapCacheOption.OnLoad;
                    //image.UriSource = new Uri(path);
                    //image.EndInit();
                    //image.Freeze();
                    //return image;
                }
                catch (Exception e)
                {
                    _ = e;
                }
            }
            return null;
        }

        private static Bitmap? TryLoadFromPath(string path)
        {
            try
            {
                if (Path.IsPathRooted(path) && File.Exists(path))
                {
                    return new Bitmap(path);
                }
            }
            catch (Exception)
            {
            }
            return null;
        }

        private static Bitmap? TryLoadFromPluginDirectory(string path, string? pluginDirectory)
        {
            if (!string.IsNullOrWhiteSpace(pluginDirectory))
                return TryLoadFromPath(Path.Combine(pluginDirectory, path))
                    ?? TryLoadFromPath(Path.Combine(pluginDirectory, "Images", Path.GetFileName(path)));
            return null;
        }

        private static Bitmap? TryLoadFromWoxDirectory(string path)
        {
            return TryLoadFromPath(Path.Combine(Constant.ProgramDirectory, path))
                ?? TryLoadFromPath(Path.Combine(Constant.ProgramDirectory, "Images", Path.GetFileName(path)));
        }

        public static Bitmap GetErrorImage()
        {
            return _errorImage;
        }

        public static Bitmap Load(string path, string pluginDirectory)
        {
            Logger.WoxDebug($"load begin {path}");
            var img = _cache.GetOrAdd(pluginDirectory + path, p => LoadInternal(path, pluginDirectory));
            Logger.WoxTrace($"load end {path}");
            return img;
        }

        /// <summary>
        /// return cache if exist,
        /// or return default image immediatly and use updateImageCallback to return new image
        /// </summary>
        /// <param name="path"></param>
        /// <param name="updateImageCallback"></param>
        /// <returns></returns>
        internal static Bitmap Load(string? path, Action<Bitmap> updateImageCallback, string title, string? pluginID, string? pluginDirectory)
        {
            Logger.WoxDebug($"load begin {path}");
            var img = _cache.GetOrAdd(pluginDirectory + path, _defaultFileImage, (string p) =>
             {
                 try
                 {
                     return LoadInternal(path, pluginDirectory);
                 }
                 catch (Exception e)
                 {
                     e.Data.Add(nameof(title), title);
                     e.Data.Add(nameof(pluginID), pluginID);
                     e.Data.Add(nameof(pluginDirectory), pluginDirectory);
                     e.Data.Add(nameof(p), p);
                     Logger.WoxError($"cannot load image async <{p}>", e);
                     return GetErrorImage();
                 }
             }, updateImageCallback
            );
            Logger.WoxTrace($"load end {path}");
            return img;
        }
    }
}