﻿using Avalonia.Media.Imaging;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Wox.Helper;
using Wox.Infrastructure.Logger;

namespace Wox.Image
{
    internal class CacheEntry
    {
        internal string Key;
        internal Bitmap Image;
        internal DateTime ExpiredDate;

        public CacheEntry(string key, Bitmap image, DateTime expiredTime)
        {
            Key = key;
            Image = image;
            ExpiredDate = expiredTime;
        }
    }

    internal class UpdateCallbackEntry
    {
        internal string Key;
        internal Func<string, Bitmap> ImageFactory;
        internal Action<Bitmap> UpdateImageCallback;

        public UpdateCallbackEntry(string key, Func<string, Bitmap> imageFactory, Action<Bitmap> updateImageCallback)
        {
            Key = key;
            ImageFactory = imageFactory;
            UpdateImageCallback = updateImageCallback;
        }
    }

    internal class ImageCache
    {
        private readonly TimeSpan _expiredTime = new TimeSpan(24, 0, 0);
        private readonly TimeSpan _checkInterval = new TimeSpan(1, 0, 0);
        private const int _cacheLimit = 500;

        private readonly ConcurrentDictionary<string, CacheEntry> _cache;
        private BlockingCollection<CacheEntry> _cacheQueue;
        private readonly SortedSet<CacheEntry> _cacheSorted;
        private BlockingCollection<UpdateCallbackEntry> _updateQueue;

        private readonly System.Threading.Timer timer;
        private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

        public ImageCache()
        {
            _cache = new ConcurrentDictionary<string, CacheEntry>();
            _cacheSorted = new SortedSet<CacheEntry>(new CacheEntryComparer());
            _cacheQueue = new BlockingCollection<CacheEntry>();
            _updateQueue = new BlockingCollection<UpdateCallbackEntry>();

            timer = new System.Threading.Timer(ExpirationCheck, null, _checkInterval, _checkInterval);
            Task.Run(() =>
            {
                while (true)
                {
                    CacheEntry entry = _cacheQueue.Take();
                    int currentCount = _cache.Count;
                    if (currentCount > _cacheLimit)
                    {
                        CacheEntry? min = _cacheSorted.Min;
                        if (min != null)
                        {
                            _cacheSorted.Remove(min);
                            bool removeResult = _cache.TryRemove(min.Key, out var cacheEntry);
                            if (removeResult)
                            {
                                cacheEntry!.Image?.Dispose();
                                Logger.WoxDebug($"remove exceed: <{removeResult}> entry: <{min.Key}>");
                            }
                        }
                    }
                    else
                    {
                        _cacheSorted.Remove(entry);
                    }
                    _cacheSorted.Add(entry);
                }
            }).ContinueWith(ErrorReporting.UnhandledExceptionHandleTask, TaskContinuationOptions.OnlyOnFaulted);
            Task.Run(() =>
            {
                while (true)
                {
                    UpdateCallbackEntry entry = _updateQueue.Take();
                    CacheEntry addEntry = Add(entry.Key, entry.ImageFactory);
                    entry.UpdateImageCallback(addEntry.Image);
                }
            }).ContinueWith(ErrorReporting.UnhandledExceptionHandleTask, TaskContinuationOptions.OnlyOnFaulted);
        }

        private void ExpirationCheck(object? state)
        {
            try
            {
                DateTime now = DateTime.Now;
                Logger.WoxDebug($"ExpirationCheck start {now}");
                List<KeyValuePair<string, CacheEntry>> pairs = _cache.Where(pair => now > pair.Value.ExpiredDate).ToList();

                foreach (KeyValuePair<string, CacheEntry> pair in pairs)
                {
                    bool success = _cache.TryRemove(pair.Key, out CacheEntry? entry);
                    Logger.WoxDebug($"remove expired: <{success}> entry: <{pair.Key}>");
                }
            }
            catch (Exception e)
            {
                e.Data.Add(nameof(state), state);
                Logger.WoxError($"error check image cache with state: {state}", e);
            }
        }

        /// <summary>
        /// Not thread safe, should be only called from ui thread
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Bitmap GetOrAdd(string key, Func<string, Bitmap> imageFactory)
        {
            if (_cache.TryGetValue(key, out CacheEntry? entry))
            {
                UpdateDate(entry);
                return entry.Image;
            }
            else
            {
                entry = Add(key, imageFactory);
                return entry.Image;
            }
        }

        public Bitmap GetOrAdd(string key, Bitmap defaultImage, Func<string, Bitmap> imageFactory, Action<Bitmap> updateImageCallback)
        {
            if (_cache.TryGetValue(key, out CacheEntry? getEntry))
            {
                UpdateDate(getEntry);
                return getEntry.Image;
            }
            else
            {
                _updateQueue.Add(new UpdateCallbackEntry(key, imageFactory, updateImageCallback));
                return defaultImage;
            }
        }

        private CacheEntry Add(string key, Func<string, Bitmap> imageFactory)
        {
            CacheEntry entry;
            Bitmap image = imageFactory(key);
            entry = new CacheEntry(key, image, DateTime.Now + _expiredTime);
            _cache[key] = entry;
            _cacheQueue.Add(entry);
            return entry;
        }

        private void UpdateDate(CacheEntry entry)
        {
            entry.ExpiredDate = DateTime.Now + _expiredTime;
            _cacheQueue.Add(entry);
        }
    }

    internal class CacheEntryComparer : IComparer<CacheEntry>
    {
        public int Compare(CacheEntry? x, CacheEntry? y)
        {
            if (x == null && y == null)
                return 0;
            if (x == null)
                return -1;
            if (y == null)
                return 1;
            return x.ExpiredDate.CompareTo(y.ExpiredDate);
        }
    }
}