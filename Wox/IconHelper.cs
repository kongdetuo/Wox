using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.IO;
using Wox.Image;
using Wox.Plugin;

namespace Wox
{
    public class IconHelper : IIconHelper
    {
        public Plugin.IconLoader FromAssociatedIcon(string filepath)
        {
            return new AssociatedIconLoader(filepath);
        }
        public Plugin.IconLoader FromImage(string filename)
        {

            return new ImageFileIconLoader(filename);
        }
 
        public Plugin.IconLoader FromEmbededIcon(string filename, int? index)
        {
            if(index.HasValue)
            return new EmbededIconLoader(filename, index.Value);
            return new EmbededIconLoader(filename);
        }

        public Plugin.IconLoader FromColor(System.Drawing.Color color)
        {
            return new ColorIconLoader(color);
        }

        private class AssociatedIconLoader : Plugin.IconLoader
        {
            public AssociatedIconLoader(string filepath)
            {
                Filepath = filepath;
            }

            public string Filepath { get; }

            public override object? Load(ImageLoadContext context)
            {
               return FileIconHelper.GetAssociatedIconImage(Filepath, 32 > 32);
            }
        }

        class EmbededIconLoader : Plugin.IconLoader
        {
            public EmbededIconLoader(string filename, int index)
            {
                Filename = filename;
                Index = index;
            }

            public EmbededIconLoader(string filename)
            {
                this.Filename = filename;

                var index = Filename.LastIndexOf(",");
                if (index != -1)
                {
                    var p1 = filename.Substring(0, index);
                    var p2 = filename.Substring(index + 1);
                    if (File.Exists(p1) && int.TryParse(p2, out var i))
                    {
                        this.Filename = p1;
                        this.Index = i;
                    }
                }

            }
            public string Filename { get; private set;}
            public int? Index { get; }

            public override object? Load(ImageLoadContext context)
            {
                if (Filename.StartsWith("@")) // 暂时不知道哪里会有这个格式
                    Filename = Filename.Substring(1);

                if (!File.Exists(Filename))
                    return null;

                if (Index.HasValue)
                    return FileIconHelper.GetEmbededIconImage(Filename, 32, Math.Abs(Index.Value));

                return FileIconHelper.GetEmbededIconImage(Filename, 32, null) ??
                        FileIconHelper.GetAssociatedIconImage(Filename, false);
            }
        }

        class ImageFileIconLoader : Plugin.IconLoader
        {
            private readonly string filename;

            public ImageFileIconLoader(string filename)
            {
                this.filename = filename;
            }

            public override object Load(ImageLoadContext context)
            {
                return ImageLoader.Load(filename, context.PluginDirectory);


                return TryLoadFromPath(filename)
                    ?? TryLoadFromPluginDirectory(filename, context)
                    ?? TryLoadFromWoxDirectory(filename, context);
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

            private static Bitmap? TryLoadFromPluginDirectory(string path, ImageLoadContext context)
            {
                if (!string.IsNullOrWhiteSpace(context.PluginDirectory))
                    return TryLoadFromPath(Path.Combine(context.PluginDirectory, path))
                        ?? TryLoadFromPath(Path.Combine(context.PluginDirectory, "Images", Path.GetFileName(path)));
                return null;
            }

            private static Bitmap? TryLoadFromWoxDirectory(string path, ImageLoadContext context)
            {
                return TryLoadFromPath(Path.Combine(context.WoxDirectory, path))
                    ?? TryLoadFromPath(Path.Combine(context.WoxDirectory, "Images", Path.GetFileName(path)));
            }
        }
        public class ColorIconLoader : Plugin.IconLoader
        {
            private readonly string filename;

            public Color Color { get; }

            public ColorIconLoader(System.Drawing.Color c)
            {
                this.Color = Color.FromArgb(c.A,c.R,c.G,c.B);
            }

            public override object Load(ImageLoadContext context)
            {
                return Color;
            }

        }
    }
}