using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Samples.Net8.Caching
{
    /// <summary>
    /// In-process implementation of <see cref="IDistributedCache"/> backed by
    /// <see cref="ConcurrentDictionary{TKey,TValue}"/>.
    /// Used when no real Redis server is available. The HVO Redis telemetry
    /// instrumentation wraps this at the <see cref="IDistributedCache"/> level,
    /// so all get/set operations still produce telemetry spans.
    /// </summary>
    public sealed class FakeRedisCache : IDistributedCache
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _store = new();
        private readonly ILogger<FakeRedisCache> _logger;
        private long _hits;
        private long _misses;
        private long _sets;
        private long _removes;

        /// <summary>
        /// Initializes a new instance of the <see cref="FakeRedisCache"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        public FakeRedisCache(ILogger<FakeRedisCache> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Total cache hits.</summary>
        public long Hits => Interlocked.Read(ref _hits);

        /// <summary>Total cache misses.</summary>
        public long Misses => Interlocked.Read(ref _misses);

        /// <summary>Total cache sets.</summary>
        public long Sets => Interlocked.Read(ref _sets);

        /// <summary>Total cache removes.</summary>
        public long Removes => Interlocked.Read(ref _removes);

        /// <summary>Current number of entries in the cache.</summary>
        public int Count => _store.Count;

        /// <inheritdoc />
        public byte[]? Get(string key)
        {
            if (_store.TryGetValue(key, out var entry) && !entry.IsExpired)
            {
                Interlocked.Increment(ref _hits);
                _logger.LogTrace("FakeRedis GET hit: {Key}", key);
                return entry.Value;
            }

            if (entry != null && entry.IsExpired)
            {
                _store.TryRemove(key, out _);
            }

            Interlocked.Increment(ref _misses);
            _logger.LogTrace("FakeRedis GET miss: {Key}", key);
            return null;
        }

        /// <inheritdoc />
        public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            return Task.FromResult(Get(key));
        }

        /// <inheritdoc />
        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            var expiration = GetAbsoluteExpiration(options);
            _store[key] = new CacheEntry(value, expiration);
            Interlocked.Increment(ref _sets);
            _logger.LogTrace("FakeRedis SET: {Key} (expires={Expiration})", key, expiration);
        }

        /// <inheritdoc />
        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options,
            CancellationToken token = default)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Refresh(string key)
        {
            // Sliding expiration is simulated by resetting the absolute time
            if (_store.TryGetValue(key, out var entry) && !entry.IsExpired)
            {
                _logger.LogTrace("FakeRedis REFRESH: {Key}", key);
            }
        }

        /// <inheritdoc />
        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            Refresh(key);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Remove(string key)
        {
            _store.TryRemove(key, out _);
            Interlocked.Increment(ref _removes);
            _logger.LogTrace("FakeRedis REMOVE: {Key}", key);
        }

        /// <inheritdoc />
        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }

        private static DateTimeOffset? GetAbsoluteExpiration(DistributedCacheEntryOptions options)
        {
            if (options.AbsoluteExpiration.HasValue)
                return options.AbsoluteExpiration;

            if (options.AbsoluteExpirationRelativeToNow.HasValue)
                return DateTimeOffset.UtcNow + options.AbsoluteExpirationRelativeToNow.Value;

            if (options.SlidingExpiration.HasValue)
                return DateTimeOffset.UtcNow + options.SlidingExpiration.Value;

            return null; // Never expires
        }

        private sealed class CacheEntry
        {
            public CacheEntry(byte[] value, DateTimeOffset? absoluteExpiration)
            {
                Value = value;
                AbsoluteExpiration = absoluteExpiration;
            }

            public byte[] Value { get; }
            public DateTimeOffset? AbsoluteExpiration { get; }
            public bool IsExpired => AbsoluteExpiration.HasValue && AbsoluteExpiration.Value < DateTimeOffset.UtcNow;
        }
    }
}
