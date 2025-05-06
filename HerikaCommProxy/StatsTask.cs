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
                    long passedRequests = _cacheTools.HitCount("passed");
                    long droppedRequests = _cacheTools.HitCount("dropped");
                    long total = passedRequests + droppedRequests;
                    double percent = 0.0;

                    if (droppedRequests > 0)
                        percent = total / droppedRequests;

                    _logger.LogInformation("{0:000000} of {1:000000} requests dropped.  {3:00.00}%", droppedRequests, total, percent);
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