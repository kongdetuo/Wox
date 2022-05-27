using System;
using System.Collections.Generic;

namespace Wox.Helper
{
    public static class SyntaxSugars
    {
        public static TResult CallOrRescueDefault<TResult>(Func<TResult> callback)
        {
            return CallOrRescueDefault(callback, default(TResult));
        }

        public static TResult CallOrRescueDefault<TResult>(Func<TResult> callback, TResult def)
        {
            try
            {
                return callback();
            }
            catch
            {
                return def;
            }
        }

        public static async IAsyncEnumerable<IList<T>> Buffer<T>(this IAsyncEnumerable<T> sources, TimeSpan time)
        {
            var abc = sources.GetAsyncEnumerator();
            bool isCompleted = false;
            while (!isCompleted)
            {
                using var source = new System.Threading.CancellationTokenSource(time);
                var list = new List<T>();
                try
                {
                    while (await abc.MoveNextAsync(source.Token))
                        list.Add(abc.Current);
                    isCompleted = true;
                }
                catch (Exception)
                {

                }
                if (list.Count > 0)
                    yield return list;
            }
        }
    }
}
