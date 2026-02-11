using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Samples.Net8.Caching
{
    /// <summary>
    /// Cache-aside service for weather data. Uses <see cref="IDistributedCache"/>
    /// (backed by <see cref="FakeRedisCache"/> in local mode, or real Redis via Docker).
    /// </summary>
    public sealed class WeatherCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<WeatherCacheService> _logger;
        private readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Initializes a new instance of the <see cref="WeatherCacheService"/> class.
        /// </summary>
        /// <param name="cache">Distributed cache implementation.</param>
        /// <param name="logger">Logger instance.</param>
        public WeatherCacheService(IDistributedCache cache, ILogger<WeatherCacheService> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets a cached value, or creates and caches it using the factory.
        /// Implements the cache-aside (lazy-load) pattern.
        /// </summary>
        /// <typeparam name="T">Type of the cached value.</typeparam>
        /// <param name="key">Cache key.</param>
        /// <param name="factory">Factory to produce the value on cache miss.</param>
        /// <param name="expiration">Optional expiration override.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cached or freshly-loaded value.</returns>
        public async Task<T?> GetOrCreateAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> factory,
            TimeSpan? expiration = null,
            CancellationToken cancellationToken = default) where T : class
        {
            // Try cache first
            var cached = await _cache.GetAsync(key, cancellationToken).ConfigureAwait(false);
            if (cached != null)
            {
                _logger.LogDebug("Cache HIT for key {CacheKey}", key);
                return Deserialize<T>(cached);
            }

            _logger.LogDebug("Cache MISS for key {CacheKey}, loading from source", key);

            // Cache miss — load from source
            var value = await factory(cancellationToken).ConfigureAwait(false);
            if (value != null)
            {
                var bytes = Serialize(value);
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration
                };

                await _cache.SetAsync(key, bytes, options, cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("Cached value for key {CacheKey} (TTL={TTL})", key, options.AbsoluteExpirationRelativeToNow);
            }

            return value;
        }

        /// <summary>
        /// Invalidates a cache entry.
        /// </summary>
        /// <param name="key">Cache key to remove.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task InvalidateAsync(string key, CancellationToken cancellationToken = default)
        {
            await _cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Invalidated cache key {CacheKey}", key);
        }

        private static byte[] Serialize<T>(T value)
        {
            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value));
        }

        private static T? Deserialize<T>(byte[] bytes)
        {
            return JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(bytes));
        }
    }
}
