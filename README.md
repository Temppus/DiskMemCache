# DiskMemCache

Minimalistic persistence and caching library. Nuget available [here](https://www.nuget.org/packages/DiskMemCache/).

## Which problem it solves ?
Library was designed to solve issue of not wasting time while developing and application that needs to do some heavy or long computation of some data and you do not want to wait for that result every time you are developing an app, but you need the data to be there.

### How it works ?
Every time you want to cache/persist result of operation you can wrap this call with:

 ```csharp
// First call with this key will call provided function and also saves the result in:
// memory cache and also on filesystem as JSON
var result = await DiskMemCache.GetOrComputeAsync("exampleKey123", () => Task.FromResult(10));

// Next calls for operation with same key will be returned from memory cache
// or will be deserialized from file if application was restarted later on
var result = await DiskMemCache.GetOrComputeAsync("exampleKey123", () => Task.FromResult(10));
 ```

### Cache invalidation

You can invalidate cache either manually by calling


```csharp
// Removes all cached entries stored in memory/files on disk
DiskMemCache.PurgeAll();

// Removes only operation with concrete key
DiskMemCache.Purge(key => key == "123");
```

or by using overload where you invalidate cache and "force" operation to be evaluated based on last caching time

```csharp
// Force cache invalidation if cached entry existed more than your conditional logic
var x = await DiskMemCache.GetOrComputeAsync(key, () => Task.FromResult(10), t =>
// in this case 5 minutes ago
 t > TimeSpan.FromMinutes(5));
```

## Note
I do not want this library to be super extendable or configurable. If you are looking for something more complex and configurable look for more mature libraries.
