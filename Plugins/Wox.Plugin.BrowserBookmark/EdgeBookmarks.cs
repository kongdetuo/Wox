using System;
using System.Collections.Generic;
using System.IO;

namespace Wox.Plugin.BrowserBookmark
{
    public class EdgeBookmarks
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
            bookmarks.AddRange(ChromiumBookmarkFile.LoadFromDirectory(Path.Combine(platformPath, @"Microsoft\Edge\User Data"), "Microsoft Edge"));
            bookmarks.AddRange(ChromiumBookmarkFile.LoadFromDirectory(Path.Combine(platformPath, @"Microsoft\Edge Dev\User Data"), "Microsoft Edge Dev"));
            bookmarks.AddRange(ChromiumBookmarkFile.LoadFromDirectory(Path.Combine(platformPath, @"Microsoft\Edge SxS\User Data"), "Microsoft Edge Canary"));
        }
    }
}