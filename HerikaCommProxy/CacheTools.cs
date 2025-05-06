using Microsoft.Extensions.Caching.Memory;

namespace HerikaCommProxy
{
    public class CacheTools
    {
        private readonly IMemoryCache _memoryCache;
        private readonly string _cacheKeyPrefix;

        public CacheTools(IMemoryCache memoryCache, string cacheKeyPrefix = "ProxyData_")
        {
            _memoryCache = memoryCache;
            _cacheKeyPrefix = cacheKeyPrefix;
        }





        /// <summary>
        /// Get the hit count for the specified key. Optionally incrementing it and returning that value.
        /// </summary>
        /// <param name="counter"></param>
        /// <param name="increment"></param>
        /// <returns></returns>
        public long HitCount(string counter, bool increment = false)
        {
            if (_memoryCache.TryGetValue(_cacheKeyPrefix + "count_" + counter, out long cachedResponse))
            {
                if (increment)
                {
                    cachedResponse++;
                    _memoryCache.Set(_cacheKeyPrefix + "count_" + counter, cachedResponse);
                }

                return cachedResponse;
            }

            if (increment)
            {
                _memoryCache.Set(_cacheKeyPrefix + "count_" + counter, 1L);
                return 1L;
            }
            else
            {
                return 0L;
            }
        }
    }
}
