using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DiskMemCache
{
    public static class DiskMemCache
    {
        private const string KeyFileExtension = ".json";

        public static readonly string DiskCacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DiskMemCache");

        static DiskMemCache()
        {
            if (!Directory.Exists(DiskCacheDir))
            {
                Directory.CreateDirectory(DiskCacheDir);
            }
        }

        private static readonly Dictionary<string, (DateTime cacheTime, object data)> MemCache = new();

        public static Task<T> GetOrComputeAsync<T>(string key, Func<T> computeFunc, CancellationToken cancellationToken = default)
        {
            return GetOrComputeAsync(key, computeFunc, null, null, cancellationToken);
        }

        public static Task<T> GetOrComputeAsync<T>(string key, Func<T> computeFunc, Predicate<TimeSpan> invalidateIf, CancellationToken cancellationToken = default)
        {
            return GetOrComputeAsync(key, computeFunc, invalidateIf, null, cancellationToken);
        }

        public static async Task<T> GetOrComputeAsync<T>(string key, Func<T> computeFunc, Predicate<TimeSpan> invalidateIf, Predicate<T> cacheIf, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var fileNameWithExt = $"{key}___{now.Ticks}{KeyFileExtension}";
            var filePath = Path.Combine(DiskCacheDir, fileNameWithExt);

            if (MemCache.TryGetValue(key, out var value))
            {
                var timeDiff = now - value.cacheTime;
                if (invalidateIf?.Invoke(timeDiff) == true)
                {
                    Purge(k => k == key);
                }
                else
                {
                    return (T)value.data;
                }
            }

            if (TryGetFile(DiskCacheDir, $"{key}___", out var file))
            {
                FileNameToKeyAndTimestamp(file, out _, out var cacheTime);

                if (invalidateIf?.Invoke(now - cacheTime) == true)
                {
                    Purge(k => k == key);
                }
                else
                {
                    var jsonStr = await File.ReadAllTextAsync(file, cancellationToken);

                    var deserializedData = JsonConvert.DeserializeObject<T>(jsonStr);
                    MemCache.TryAdd(key, (now, deserializedData));
                    return deserializedData;
                }
            }

            var data = computeFunc();

            if (cacheIf != null && !cacheIf(data))
            {
                return data;
            }

            MemCache.TryAdd(key, (now, data));
            await File.WriteAllTextAsync(filePath, JsonConvert.SerializeObject(data), cancellationToken);

            return data;
        }

        private static bool TryGetFile(string directoryPath, string fileNameStartWith, out string file)
        {
            file = null;

            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException($"Directory '{directoryPath}' not found.");
            }

            string[] files = Directory.GetFiles(directoryPath, $"{fileNameStartWith}*");

            bool exists = files.Length > 0;

            if (exists)
            {
                file = files[0];
            }

            return exists;
        }

        public static void PurgeAll()
        {
            Purge(null as Predicate<string>);
        }

        public static void Purge(Predicate<string> keyPredicate = null)
        {
            if (keyPredicate == null)
            {
                Purge(null as Func<string, TimeSpan, bool>);
                return;
            }

            Purge((key, _) => keyPredicate(key));
        }

        public static void Purge(Func<string, TimeSpan, bool> keyPredicate = null)
        {
            var now = DateTime.UtcNow;

            if (keyPredicate == null)
            {
                MemCache.Clear();

                foreach (var file in Directory.GetFiles(DiskCacheDir))
                {
                    File.Delete(file);
                }

                return;
            }

            foreach (var kv in MemCache)
            {
                var diff = now - kv.Value.cacheTime;

                if (keyPredicate(kv.Key, diff))
                {
                    MemCache.Remove(kv.Key);
                }
            }

            foreach (var file in Directory.GetFiles(DiskCacheDir))
            {
                FileNameToKeyAndTimestamp(file, out var key, out var cacheTime);

                var diff = now - cacheTime;

                if (keyPredicate(key, diff))
                {
                    File.Delete(Path.Combine(DiskCacheDir, Path.GetFileName(file)));
                }
            }
        }

        private static void FileNameToKeyAndTimestamp(string file, out string key, out DateTime timestamp)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            var split = fileName.Split("___");

            key = split[0];
            timestamp = new DateTime(long.Parse(split[1]));
        }
    }
}
