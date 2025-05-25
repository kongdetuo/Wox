using Avalonia.Platform;
using Avalonia;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Vortice.WIC;
using Wox.Infrastructure.Logger;
namespace Wox.Image
{
    static class EmbededIcon
    {
        private delegate bool EnumResNameDelegate(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam);
        [DllImport("kernel32.dll", EntryPoint = "EnumResourceNamesW", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool EnumResourceNamesWithID(IntPtr hModule, uint lpszType, EnumResNameDelegate lpEnumFunc, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool FreeLibrary(IntPtr hModule);
        private const uint GROUP_ICON = 14;
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr LoadImage(IntPtr hinst, IntPtr lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        extern static bool DestroyIcon(IntPtr handle);

        private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

        public static Avalonia.Media.Imaging.Bitmap? GetImage(string key, string path, int iconSize)
        {
            // https://github.com/CoenraadS/Windows-Control-Panel-Items/
            // https://gist.github.com/jnm2/79ed8330ceb30dea44793e3aa6c03f5b

            string iconStringRaw = path.Substring(key.Length);
            var iconString = new List<string>(iconStringRaw.Split([','], 2));
            IntPtr iconPtr = IntPtr.Zero;
            IntPtr dataFilePointer;
            IntPtr iconIndex;
            uint LOAD_LIBRARY_AS_DATAFILE = 0x00000002;

            if (string.IsNullOrEmpty(iconString[0]))
            {
                var e = new ArgumentException($"iconString empth {path}");
                e.Data.Add(nameof(path), path);
                throw e;
            }

            if (iconString[0][0] == '@')
            {
                iconString[0] = iconString[0].Substring(1);
            }

            dataFilePointer = LoadLibraryEx(iconString[0], IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);
            if (iconString.Count == 2)
            {
                // C:\WINDOWS\system32\mblctr.exe,0
                // %SystemRoot%\System32\FirewallControlPanel.dll,-1
                var index = Math.Abs(int.Parse(iconString[1]));
                iconIndex = index;
                iconPtr = LoadImage(dataFilePointer, iconIndex, 1, iconSize, iconSize, 0);
            }

            if (iconPtr == IntPtr.Zero)
            {
                IntPtr defaultIconPtr = IntPtr.Zero;
                var callback = new EnumResNameDelegate((hModule, lpszType, lpszName, lParam) =>
                {
                    defaultIconPtr = lpszName;
                    return false;
                });
                var result = EnumResourceNamesWithID(dataFilePointer, GROUP_ICON, callback, IntPtr.Zero); //Iterate through resources. 
                if (!result)
                {
                    int error = Marshal.GetLastWin32Error();
                    int userStoppedResourceEnumeration = 0x3B02;
                    if (error != userStoppedResourceEnumeration)
                    {
                        Win32Exception exception = new Win32Exception(error);
                        throw exception;
                    }
                }
                iconPtr = LoadImage(dataFilePointer, defaultIconPtr, 1, iconSize, iconSize, 0);
            }

            FreeLibrary(dataFilePointer);
            return CreateBitmapSourceFromHIcon(iconPtr);
        }

        public static Avalonia.Media.Imaging.Bitmap? CreateBitmapSourceFromHIcon(nint ptr)
        {
            if (ptr != IntPtr.Zero)
            {
                
                using var factory = new IWICImagingFactory();
                using var bitmap = factory.CreateBitmapFromHICON(ptr);
                var result = bitmap.ToAvaloniaBitmap();
                return result;
            }
            return null;
        }

        public static Avalonia.Media.Imaging.Bitmap? ToAvaloniaBitmap(this IWICBitmap? bitmap)
        {
            if (bitmap == null)
            {
                return null;
            }
            using var wicBitmapLock = bitmap.Lock(BitmapLockFlags.Read);

            wicBitmapLock.GetSize(out var width, out var height);

            return new Avalonia.Media.Imaging.Bitmap(
                    Avalonia.Platform.PixelFormat.Bgra8888,
                    AlphaFormat.Unpremul,
                    wicBitmapLock.Data.DataPointer,
                    new PixelSize((int)width, (int)height),
                    new Avalonia.Vector(96, 96),
                    (int)wicBitmapLock.Stride
                );

        }

    }
}
