using System.Collections.Generic;

namespace Wox.Plugin
{
    public static class LinqEx
    {
        public static IEnumerable<T> WhereNotNull<T>(IEnumerable<T?> sources)
        {
            foreach (var item in sources)
            {
                if (item is not null)
                    yield return item;
            }
        }
    }
}