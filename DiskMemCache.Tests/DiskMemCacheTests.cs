using System.Diagnostics;
using Temppus.Caching;

// ReSharper disable once CheckNamespace
namespace Temppus.Tests
{
    public class DiskMemCacheTests : IDisposable
    {
        public DiskMemCacheTests()
        {
            DiskMemCache.PurgeAll();
        }

        public class Item
        {
            public int Value { get; set; }
        }

        private static Task<Item> ComputeItem10() => Task.FromResult(new Item { Value = 10 });
        private static Task<Item> ComputeItem20() => Task.FromResult(new Item { Value = 20 });

        private static async Task<Item> LongRunningOperationFunctionToCacheAsync()
        {
            // expensive computation / long IO operation
            await Task.Delay(1000);
            return new Item { Value = 9000 };
        }

        [Fact]
        public async Task Test_Caching_Example()
        {
            // first operation with this key will call function and cache result in memory cache + also on file as JSON
            var result = await DiskMemCache.GetOrComputeAsync("my-expensive-func", LongRunningOperationFunctionToCacheAsync);
            Assert.Equal(9000, result.Value);

            var sw = Stopwatch.StartNew();

            // all next results are returned from cache
            // if cached item spend in cache is less than 1 hour
            for (int i = 0; i < 100; i++)
            {
                var resultFromMemCache = await DiskMemCache.GetOrComputeAsync("my-expensive-func", 
                    LongRunningOperationFunctionToCacheAsync,
                    t => t > TimeSpan.FromHours(1));

                Assert.Equal(9000, result.Value);
            }

            // process re-started (assuming less than 1 hour from item  being cached)
            // result will be returned from serialized file and not computed again
            var resultReturnedFromSerializedFile = await DiskMemCache.GetOrComputeAsync("my-expensive-func",
                LongRunningOperationFunctionToCacheAsync,
                t => t > TimeSpan.FromHours(1));

            Assert.Equal(9000, result.Value);

            sw.Stop();

            Assert.True(sw.Elapsed < TimeSpan.FromSeconds(2));
        }

        [Fact]
        public async Task Test_Caching_Simple()
        {
            var key = Guid.NewGuid().ToString();

            var x = await DiskMemCache.GetOrComputeAsync(key, ComputeItem10);
            Assert.Equal(10, x.Value);
            x = await DiskMemCache.GetOrComputeAsync(key, ComputeItem20);
            Assert.Equal(10, x.Value);
            x = await DiskMemCache.GetOrComputeAsync(key, ComputeItem20, t => t > TimeSpan.FromMilliseconds(500));
            Assert.Equal(10, x.Value);

            await Task.Delay(500);
            x = await DiskMemCache.GetOrComputeAsync(key, ComputeItem20, t => t > TimeSpan.FromMilliseconds(100));
            Assert.Equal(20, x.Value);
        }

        [Fact]
        public async Task Test_Caching_Scalar()
        {
            var key = Guid.NewGuid().ToString();

            var x = await DiskMemCache.GetOrComputeAsync(key, () => Task.FromResult(9));
            Assert.Equal(9, x);
            x = await DiskMemCache.GetOrComputeAsync(key, () => Task.FromResult(9));
            Assert.Equal(9, x);
        }

        [Fact]
        public async Task Test_Caching_Keys()
        {
            DiskMemCache.PurgeAll();

            {
                var key1 = Guid.NewGuid().ToString();

                var x = await DiskMemCache.GetOrComputeAsync(key1, ComputeItem10);
                Assert.Equal(10, x.Value);

                x = await DiskMemCache.GetOrComputeAsync(key1, ComputeItem20);
                Assert.Equal(10, x.Value);
            }

            {
                var key2 = Guid.NewGuid().ToString();

                var y = await DiskMemCache.GetOrComputeAsync(key2, ComputeItem20);
                Assert.Equal(20, y.Value);

                y = await DiskMemCache.GetOrComputeAsync(key2, ComputeItem10);
                Assert.Equal(20, y.Value);
            }
        }

        [Fact]
        public async Task Test_Cache_Purging()
        {
            var key1 = Guid.NewGuid().ToString();

            var x = await DiskMemCache.GetOrComputeAsync(key1, ComputeItem10);
            Assert.Equal(10, x.Value);
            x = await DiskMemCache.GetOrComputeAsync(key1, ComputeItem20);
            Assert.Equal(10, x.Value);

            var key2 = Guid.NewGuid().ToString();

            var y = await DiskMemCache.GetOrComputeAsync(key2, ComputeItem10);
            Assert.Equal(10, y.Value);
            y = await DiskMemCache.GetOrComputeAsync(key2, ComputeItem20);
            Assert.Equal(10, y.Value);

            DiskMemCache.Purge(k => k == key1);

            x = await DiskMemCache.GetOrComputeAsync(key1, ComputeItem20);
            Assert.Equal(20, x.Value);

            y = await DiskMemCache.GetOrComputeAsync(key2, ComputeItem20);
            Assert.Equal(10, y.Value);
        }

        public void Dispose()
        {
            DiskMemCache.PurgeAll();
        }
    }
}