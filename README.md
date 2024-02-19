# DiskMemCache

Minimalistic persistence and caching library. Nuget available [here](https://www.nuget.org/packages/DiskMemCache/).

## Which problem it solves ?
Library was designed to solve issue of not wasting time while developing and application that needs to do some heavy or long computation of some data and you do not want to wait for that result every time you are developing an app (hot reloading or restarting and app), but you need the data to be there.

### TLDR
Every time you want to cache/persist result of operation you can wrap this call with:

 ```csharp
private static async Task<Item> LongRunningOperationFunctionToCacheAsync()
{
    // expensive computation / long IO operation ...
    await Task.Delay(TimeSpan.FromSeconds(30));
    return new Item { Value = 9000 };
}

// first operation with this key will call function and cache result in memory cache + also on file as JSON
var result = await DiskMemCache.GetOrComputeAsync("my-expensive-func", LongRunningOperationFunctionToCacheAsync);

// all next results are returned from cache
// if cached item is than 1 hour old
for (int i = 0; i < 100; i++)
{
    var resultFromMemCache = await DiskMemCache.GetOrComputeAsync("my-expensive-func", LongRunningOperationFunctionToCacheAsync,
        t => t > TimeSpan.FromHours(1));
}

// process can be re-started and result will be still return from persitent cache
// if cached item is than 1 hour old
var resultReturnedFromSerializedFile = await DiskMemCache.GetOrComputeAsync("my-expensive-func", LongRunningOperationFunctionToCacheAsync,
    t => t > TimeSpan.FromHours(1));
 ```

See [tests](DiskMemCache.Tests/DiskMemCacheTests.cs) for more examples.

### Manual cache invalidation

You can invalidate cache either manually by calling


```csharp
// Removes all cached entries stored in memory/files on disk
DiskMemCache.PurgeAll();

// Removes only operation with concrete key
DiskMemCache.Purge(key => key == "123");
```

## Note
I do not want this library to be super extendable or configurable. If you are looking for something more complex and configurable look for more mature libraries.
