using System;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.WindowsAPICodePack.Shell;
using NLog;
using Wox.Infrastructure;
using Wox.Infrastructure.Logger;

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

        private static ImageCache _cache;

        private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();
        private static ImageSource _defaultFileImage;
        private static ImageSource _errorImage;

        public static void Initialize()
        {
            string defaultFilePath = Path.Combine(Constant.ImagesDirectory, "file.png");
            _defaultFileImage = new BitmapImage(new Uri(defaultFilePath))
            {
                DecodePixelHeight = 32,
                DecodePixelWidth = 32
            };
            _defaultFileImage.Freeze();
            _errorImage = new BitmapImage(new Uri(Constant.ErrorIcon))
            {
                DecodePixelHeight = 32,
                DecodePixelWidth = 32
            };
            _errorImage.Freeze();
            _cache = new ImageCache();
        }

        private static ImageSource LoadInternal(string path, string pluginDirectory)
        {
            Logger.WoxDebug($"load from disk {path}");

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

        private static ImageSource LoadFileThumbnail(string path)
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
                    var image = shell.Thumbnail.SmallBitmapSource;
                    image.Freeze();
                    return image;
                }
                catch (ShellException e)
                {
                }
            }
            return null;
        }

        private static ImageSource LoadDirectoryThumbnail(string path)
        {
            if (Directory.Exists(path))
            {
                try
                {
                    // can be extended to support guid things
                    using ShellObject shell = ShellFile.FromParsingName(path);
                    var image = shell.Thumbnail.SmallBitmapSource;
                    image.Freeze();
                    return image;
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

        private static ImageSource LoadNormalImage(string path, string pluginDirectory)
        {
            bool normalImage = ImageExtensions.Any(e => path.EndsWith(e));

            if (normalImage)
            {
                if (TryLoadFromPath(path, out var image))
                    return image;

                if (!string.IsNullOrEmpty(pluginDirectory) && TryLoadFromPluginDirectory(path, pluginDirectory, out image))
                    return image;

                if (TryLoadFromWoxDirectory(path, out image))
                    return image;
            }
            return null;
        }

        private static ImageSource LoadEmbededImage(string path)
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

        private static ImageSource LoadDataImage(string path)
        {
            if (path.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    return new BitmapImage(new Uri(path))
                    {
                        DecodePixelHeight = 32,
                        DecodePixelWidth = 32
                    };
                }
                catch (Exception e)
                {
                }
            }
            return null;
        }

        private static bool TryLoadFromPath(string path, out ImageSource source)
        {
            try
            {
                if (Path.IsPathRooted(path) && File.Exists(path))
                {
                    source = new BitmapImage(new Uri(path))
                    {
                        DecodePixelHeight = 32,
                        DecodePixelWidth = 32
                    };
                    source.Freeze();
                    return true;
                }
            }
            catch (Exception)
            {
            }
            source = null;
            return false;
        }

        private static bool TryLoadFromPluginDirectory(string path, string pluginDirectory, out ImageSource image)
        {
            if (TryLoadFromPath(Path.Combine(pluginDirectory, path), out image))
                return true;
            if (TryLoadFromPath(Path.Combine(pluginDirectory, "Images", Path.GetFileName(path)), out image))
                return true;
            image = null;
            return false;
        }

        private static bool TryLoadFromWoxDirectory(string path, out ImageSource image)
        {
            if (TryLoadFromPath(Path.Combine(Constant.ProgramDirectory, path), out image))
                return true;
            if (TryLoadFromPath(Path.Combine(Constant.ProgramDirectory, "Images", Path.GetFileName(path)), out image))
                return true;
            image = null;
            return false;
        }

        public static ImageSource GetErrorImage()
        {
            return _errorImage;
        }

        public static ImageSource Load(string path, string pluginDirectory)
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
        internal static ImageSource Load(string path, Action<ImageSource> updateImageCallback, string title, string pluginID, string pluginDirectory)
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