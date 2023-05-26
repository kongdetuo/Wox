//using BinaryAnalysis.UnidecodeSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Wox.Plugin.BrowserBookmark
{
    public class Bookmark : IEquatable<Bookmark>, IEqualityComparer<Bookmark>
    {
        public required string Name { get; set; }
        public required string Url { get; set; }

        public bool Equals(Bookmark? other)
        {
            return Equals(this, other);
        }

        public bool Equals(Bookmark? x, Bookmark? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null)
                return false;

            return x.Name == y.Name && x.Url == y.Url;
        }

        public int GetHashCode(Bookmark bookmark)
        {
            if (bookmark is null) return 0;
            int hashName = bookmark.Name == null ? 0 : bookmark.Name.GetHashCode();
            int hashUrl = bookmark.Url == null ? 0 : bookmark.Url.GetHashCode();
            return hashName ^ hashUrl;
        }

        public override int GetHashCode()
        {
            return GetHashCode(this);
        }
    }
}
