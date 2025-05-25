using System;
using System.Runtime.InteropServices;
using Vortice.WIC;
using Wox.Image;
using Avalonia.Media.Imaging;
using Microsoft.Win32.SafeHandles;
using Windows.Win32.Foundation;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.System.LibraryLoader;
using System.Xml.Linq;
using Vortice.Direct2D1.Effects;
using Windows.Win32.UI.Shell;

namespace Wox
{
    public partial class FileIconHelper
    {
        private const uint GROUP_ICON = 14;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        extern static bool DestroyIcon(IntPtr handle);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, SHGFI uFlags);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        [Flags]
        enum SHGFI : uint
        {
            /// <summary>get icon</summary>
            Icon = 0x000000100,
            /// <summary>get display name</summary>
            DisplayName = 0x000000200,
            /// <summary>get type name</summary>
            TypeName = 0x000000400,
            /// <summary>get attributes</summary>
            Attributes = 0x000000800,
            /// <summary>get icon location</summary>
            IconLocation = 0x000001000,
            /// <summary>return exe type</summary>
            ExeType = 0x000002000,
            /// <summary>get system icon index</summary>
            SysIconIndex = 0x000004000,
            /// <summary>put a link overlay on icon</summary>
            LinkOverlay = 0x000008000,
            /// <summary>show icon in selected state</summary>
            Selected = 0x000010000,
            /// <summary>get only specified attributes</summary>
            Attr_Specified = 0x000020000,
            /// <summary>get large icon</summary>
            LargeIcon = 0x000000000,
            /// <summary>get small icon</summary>
            SmallIcon = 0x000000001,
            /// <summary>get open icon</summary>
            OpenIcon = 0x000000002,
            /// <summary>get shell size icon</summary>
            ShellIconSize = 0x000000004,
            /// <summary>pszPath is a pidl</summary>
            PIDL = 0x000000008,
            /// <summary>use passed dwFileAttribute</summary>
            UseFileAttributes = 0x000000010,
            /// <summary>apply the appropriate overlays</summary>
            AddOverlays = 0x000000020,
            /// <summary>Get the index of the overlay in the upper 8 bits of the iIcon</summary>
            OverlayIndex = 0x000000040,
        }

        static uint szSHFILEINFO = (uint)Marshal.SizeOf<SHFILEINFO>();

        public static Bitmap? GetEmbededIconImage(string path, int iconSize, int? iconIndex)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            if (path[0] == '@')
            {
                path = path.Substring(1);
            }

            if (iconIndex.HasValue)
            {
                var iconPtr1 = LoadImageByIndex(path, iconIndex.Value, GDI_IMAGE_TYPE.IMAGE_ICON, iconSize, iconSize, IMAGE_FLAGS.LR_SHARED);
                if (iconPtr1 != null)
                {
                    return iconPtr1;
                }
            }

            using var handle = PInvoke.LoadLibraryEx(path,LOAD_LIBRARY_FLAGS.LOAD_LIBRARY_AS_DATAFILE);
            PWSTR defaultImageIndex = new PWSTR();
            var result = PInvoke.EnumResourceNames((HMODULE)handle.DangerousGetHandle(), MAKEINTRESOURCEA(GROUP_ICON), new ENUMRESNAMEPROCW((HMODULE hModule, PCWSTR lpType, PWSTR lpName, nint lParam) =>
            {
                defaultImageIndex = lpName;
                return false;
            }), 0);

            if (!(bool)result)
            {
                int error = Marshal.GetLastWin32Error();
                if (error != Win32ErrorCodes.ERROR_SUCCESS && error != Win32ErrorCodes.ERROR_RESOURCE_ENUM_USER_STOP)
                {
                    return null;
                }
            }

            var imagehandle = PInvoke.LoadImage((HINSTANCE)handle.DangerousGetHandle(), defaultImageIndex, GDI_IMAGE_TYPE.IMAGE_ICON, iconSize, iconSize, IMAGE_FLAGS.LR_SHARED);
            return LoadImageFromHandle(imagehandle);
        }

        public static Bitmap? GetAssociatedIconImage(string path, bool large)
        {
            SHFILEINFO shFileInfo = new();
            SHGFI flags = SHGFI.Icon;

            if (large)
                flags |= SHGFI.LargeIcon;

            SHGetFileInfo(path, 0, ref shFileInfo, szSHFILEINFO, flags);

            IntPtr hIcon = shFileInfo.hIcon;


            return EmbededIcon.CreateBitmapSourceFromHIcon(hIcon);
        }
        unsafe static PCWSTR MAKEINTRESOURCEA(int i) => (char*)i;
        unsafe static PCWSTR MAKEINTRESOURCEA(uint i) => (char*)i;
        private unsafe static Bitmap? LoadImageByIndex(string path, int index, GDI_IMAGE_TYPE type, int cx, int cy, IMAGE_FLAGS fuLoad)
        {
            using var handle = PInvoke.LoadLibraryEx(path, LOAD_LIBRARY_FLAGS.LOAD_LIBRARY_AS_DATAFILE);
            var imageHandle = PInvoke.LoadImage((HINSTANCE)handle.DangerousGetHandle(), MAKEINTRESOURCEA(index), type, cx, cy, fuLoad);
            return LoadImageFromHandle(imageHandle);
        }

        private unsafe static Bitmap? LoadImageFromHandle(HANDLE handle)
        {
            if (!handle.IsNull)
            {
                using var factory = new IWICImagingFactory();
                using var bitmap = factory.CreateBitmapFromHICON(handle);
                var bmp = bitmap.ToAvaloniaBitmap();
                return bmp;
            }
            return null;
        }

        /// <summary>
        /// Debug system error codes
        /// </summary>
        /// <see cref="https://learn.microsoft.com/en-us/windows/win32/debug/system-error-codes"/>
        static class Win32ErrorCodes
        {
            /// <summary>
            /// The operation completed successfully.
            /// </summary>
            public static int ERROR_SUCCESS = 0;

            /// <summary>
            /// User stopped resource enumeration.
            /// </summary>
            public static int ERROR_RESOURCE_ENUM_USER_STOP = 0x3B02;
        }
    }

    
}