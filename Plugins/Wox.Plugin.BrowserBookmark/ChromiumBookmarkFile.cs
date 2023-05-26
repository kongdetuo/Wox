﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Wox.Plugin.BrowserBookmark
{
    public class ChromiumBookmarkFile
    {
        public static IEnumerable<Bookmark> LoadFromDirectory(string path, string name)
        {
            if (!Directory.Exists(path))
                return Enumerable.Empty<Bookmark>();

            var paths = Directory.GetFiles(path, "Bookmarks", new EnumerationOptions() { RecurseSubdirectories = true });
            return paths.SelectMany(path => ParseChromeBookmarks(path, name));
        }

        public static IEnumerable<Bookmark> ParseChromeBookmarks(string path, string source)
        {
            if (!File.Exists(path))
                return Enumerable.Empty<Bookmark>();

            string all = File.ReadAllText(path);
            JObject json = JObject.Parse(all);
            var items = json["roots"]?["bookmark_bar"] as JObject;
            if (items is null)
                return Enumerable.Empty<Bookmark>();
            var flatterned = GetNestedChildren(items);
            var bs = from item in flatterned
                     select new Bookmark()
                     {
                         Name = item["name"]?.Value<string>() ?? "",
                         Url = item["url"]?.Value<string>() ?? "",
                     };
            var filtered = bs.Where(IsBookmark);

            return filtered;
        }

        private static IEnumerable<JObject> GetNestedChildren(JObject jo)
        {
            List<JObject> nested = new List<JObject>();
            JArray? children = jo["children"] as JArray;
            if (children is not null)
            {
                foreach (JObject c in children.OfType<JObject>())
                {
                    var type = c["type"]?.ToString();
                    if (type == "folder")
                    {
                        var nc = GetNestedChildren(c);
                        nested.AddRange(nc);
                    }
                    else if (type == "url")
                    {
                        nested.Add(c);
                    }
                }
            }
            return nested;
        }
        private static bool IsBookmark(Bookmark bookmark)
        {
            return !bookmark.Url.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)
                && !bookmark.Url.StartsWith("vbscript:", StringComparison.OrdinalIgnoreCase);
        }
    }
}