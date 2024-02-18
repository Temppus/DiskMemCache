namespace DiskMemCache.Tests
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

        private static Item ComputeItem10() => new() { Value = 10 };
        private static Item ComputeItem20() => new() { Value = 20 };

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

            var x = await DiskMemCache.GetOrComputeAsync(key, () => 9);
            Assert.Equal(9, x);
            x = await DiskMemCache.GetOrComputeAsync(key, () => 10);
            Assert.Equal(9, x);
        }

        [Fact]
        public async Task Test_Caching_Keys()
        {
            DiskMemCache.PurgeAll();

            var x = await DiskMemCache.GetOrComputeAsync(Guid.NewGuid().ToString(), ComputeItem10);
            Assert.Equal(10, x.Value);
            x = await DiskMemCache.GetOrComputeAsync(Guid.NewGuid().ToString(), ComputeItem10);
            Assert.Equal(10, x.Value);
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