using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Wox.Plugin.BrowserBookmark
{
    public class ChromeBookmarks
    {
        private List<Bookmark> bookmarks = new List<Bookmark>();

        public List<Bookmark> GetBookmarks()
        {
            LoadChromeBookmarks();
            return bookmarks;
        }

        private void LoadChromeBookmarks()
        {
            String platformPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            bookmarks.AddRange(ChromiumBookmarkFile.LoadFromDirectory(Path.Combine(platformPath, @"Google\Chrome\User Data"), "Google Chrome"));
            bookmarks.AddRange(ChromiumBookmarkFile.LoadFromDirectory(Path.Combine(platformPath, @"Google\Chrome SxS\User Data"), "Google Chrome Canary"));
            bookmarks.AddRange(ChromiumBookmarkFile.LoadFromDirectory(Path.Combine(platformPath, @"Chromium\User Data"), "Chromium"));
        }

    }
}