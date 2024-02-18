# DiskMemCache

Looking for minimalistic caching and persistence library for your long running operations ?

## Which problem it solves ?
Library was designed to solve issue of wasted time while developing and application that needs to do some heavy or long computation of some data and you do not want to wait for that result every time.

### How it works ?
Every time you want to cache/persist result of operation you can wrap this call with:

 ```csharp
// First call with this key will trigger computation and also saves the result inside in memory cache and also on filesytem via JSON serialization
var result = await DiskMemCache.GetOrComputeAsync("key-123", () => 9);
Assert.Equal(9, x);

// Next call for this operation with same key will be returned from cache or will be deserialized from file if application was restarted later on.
result = await DiskMemCache.GetOrComputeAsync(key, () => 10);
Assert.Equal(9, x);
 ```

### Cache invalidation

You can invalidate cache either manually


```csharp
// Removes all cached entries in memory or in files
DiskMemCache.PurgeAll();

// Removes only operation with concrete key
DiskMemCache.Purge(key => key == "123");
```

or you can specify conditional lambda in each wrapped call to force computation again after some specified time


```csharp
// Each call will evaluate condition based on timespan from last caching time
// and will invalidate cache and operation will be recomputate again
// (in this example if entry is older than 5 minutes).
var x = await DiskMemCache.GetOrComputeAsync(key, () => 9,
 t => t > TimeSpan.FromMinutes(5));
```

## Note
I do not want this library to be super extendable or configurable. If you are looking for solution to quickly cache/state result of operation, maybe it will suite you.