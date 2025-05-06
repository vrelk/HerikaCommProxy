using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HerikaCommProxy
{
    public class StatsTask : BackgroundService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<StatsTask> _logger;
        private readonly CacheTools _cacheTools;


        public StatsTask(IMemoryCache memoryCache, ILogger<StatsTask> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
            _cacheTools = new CacheTools(memoryCache);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    long totalRequests = _cacheTools.HitCount("total");
                    long droppedRequests = _cacheTools.HitCount("dropped");
                    double percent = 0.0;

                    if (droppedRequests > 0)
                        percent = ((double)droppedRequests / (double)totalRequests) * 100;

                    _logger.LogInformation("{0:000000} of {1:000000} requests dropped.  {2:00.00}%", droppedRequests, totalRequests, percent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in background task");
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}