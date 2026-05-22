using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;

namespace Framework;

internal class MemoryCache : Microsoft.Extensions.Caching.Memory.MemoryCache, IMemoryCache
{
    private readonly AsyncLock _asyncLock;
    private readonly string _keyRegion;

    public MemoryCache(string keyRegion) : base(Options.Create(new MemoryCacheOptions()))
    {
        _keyRegion = keyRegion;
        _asyncLock = new();
    }

    public ValueTask ClearAsync()
    {
        Clear();
        return ValueTask.CompletedTask;
    }

    public bool Exists(string key)
        => TryGetValue(GetKeyWithPrefix(key), out _);

    public ValueTask<bool> ExistsAsync(string key)
        => ValueTask.FromResult(TryGetValue(GetKeyWithPrefix(key), out _));

    public T Get<T>(string key)
        => TryGetValue<T>(GetKeyWithPrefix(key), out var result) ? result : default;

    public ValueTask<T> GetAsync<T>(string key)
        => ValueTask.FromResult(Get<T>(GetKeyWithPrefix(key)));

    public T GetOrAdd<T>(
        string key,
        Func<T> valueFactory,
        DateTimeOffset? absoluteExpiration = null,
        TimeSpan? absoluteExpirationFromNow = null,
        TimeSpan? slidingExpiration = null
    )
    {
        if (TryGetValue(key, out T value))
        {
            return value;
        }

        using (_asyncLock.Lock())
        {
            if (TryGetValue(key, out value))
            {
                return value;
            }

            var newValue = valueFactory();

            SetInternal(
                key,
                newValue,
                absoluteExpiration: absoluteExpiration,
                absoluteExpirationFromNow: absoluteExpirationFromNow,
                slidingExpiration: slidingExpiration
            );

            return newValue;
        }
    }

    public T GetOrAdd<T>(string key, Func<(T, DateTimeOffset)> valueAndExpirationFactory)
    {
        if (TryGetValue(key, out T value))
        {
            return value;
        }

        using (_asyncLock.Lock())
        {
            if (TryGetValue(key, out value))
            {
                return value;
            }

            var (newValue, absoluteExpiration) = valueAndExpirationFactory();
            SetInternal(key, newValue, absoluteExpiration);
            return newValue;
        }
    }

    public async ValueTask<T> GetOrAddAsync<T>(
        string key,
        Func<ValueTask<T>> valueFactory,
        DateTimeOffset? absoluteExpiration = null,
        TimeSpan? absoluteExpirationFromNow = null,
        TimeSpan? slidingExpiration = null
    )
    {
        if (TryGetValue(key, out T value))
        {
            return value;
        }

        using (await _asyncLock.LockAsync())
        {
            if (TryGetValue(key, out value))
            {
                return value;
            }

            var newValue = await valueFactory();

            SetInternal(
                key,
                newValue,
                absoluteExpiration: absoluteExpiration,
                absoluteExpirationFromNow: absoluteExpirationFromNow,
                slidingExpiration: slidingExpiration
            );

            return newValue;
        }
    }

    public async ValueTask<T> GetOrAddAsync<T>(string key, Func<ValueTask<(T, DateTimeOffset)>> valueAndExpirationFactory)
    {
        if (TryGetValue(key, out T value))
        {
            return value;
        }

        using (await _asyncLock.LockAsync())
        {
            if (TryGetValue(key, out value))
            {
                return value;
            }

            var (newValue, absoluteExpiration) = await valueAndExpirationFactory();
            SetInternal(key, newValue, absoluteExpiration);
            return newValue;
        }
    }

    public long GetSize() => Count;

    public ValueTask<long> GetSizeAsync()
        => ValueTask.FromResult(GetSize());

    public void Remove(string key)
        => base.Remove(GetKeyWithPrefix(key));

    public ValueTask RemoveAsync(string key)
    {
        Remove(GetKeyWithPrefix(key));
        return ValueTask.CompletedTask;
    }

    public void Set(string key, object item)
        => SetInternal(key, item, absoluteExpiration: DateTimeOffset.MaxValue);

    public void Set(string key, object item, DateTime absoluteExpiration)
        => SetInternal(key, item, absoluteExpiration: absoluteExpiration);

    public void Set(string key, object item, TimeSpan slidingExpiration)
        => SetInternal(key, item, slidingExpiration: slidingExpiration);

    public void Set(string key, object item, DateTimeOffset absoluteExpiration)
        => SetInternal(key, item, absoluteExpiration: absoluteExpiration);

    public ValueTask SetAsync(string key, object item)
    {
        SetInternal(key, item, absoluteExpiration: DateTimeOffset.MaxValue);
        return ValueTask.CompletedTask;
    }

    public ValueTask SetAsync(string key, object item, DateTime absoluteExpiration)
    {
        SetInternal(key, item, absoluteExpiration: absoluteExpiration);
        return ValueTask.CompletedTask;
    }

    public ValueTask SetAsync(string key, object item, TimeSpan slidingExpiration)
    {
        SetInternal(key, item, slidingExpiration: slidingExpiration);
        return ValueTask.CompletedTask;
    }

    public bool TryGetValue<T>(string key, out T value)
    {
        if (TryGetValue(GetKeyWithPrefix(key), out var result))
        {
            value = (T)result;
            return true;
        }

        value = default;
        return false;
    }

    private string GetKeyWithPrefix(string key) => key.StartsWith(_keyRegion) ? key : $"{_keyRegion}_{key}";

    private void SetInternal(
        string key,
        object item,
        DateTimeOffset? absoluteExpiration = null,
        TimeSpan? absoluteExpirationFromNow = null,
        TimeSpan? slidingExpiration = null
    )
    {
        using var entry = CreateEntry(GetKeyWithPrefix(key)).SetValue(item);
        if (absoluteExpiration is not null)
        {
            entry.SetAbsoluteExpiration(absoluteExpiration.Value);
        }
        else if (absoluteExpirationFromNow is not null)
        {
            entry.SetAbsoluteExpiration(absoluteExpirationFromNow.Value);
        }
        else if (slidingExpiration is not null)
        {
            entry.SetSlidingExpiration(slidingExpiration.Value);
        }
    }
}