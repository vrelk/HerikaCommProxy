using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Text;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Forwarder;
using System.Web;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace HerikaCommProxy.Controllers
{
    [ApiController]
    [Route("HerikaServer")]
    public class ConditionalProxyController : ControllerBase
    {
        private readonly IHttpForwarder _httpForwarder;
        private readonly IMemoryCache _memoryCache;
        private readonly HttpMessageInvoker _httpClient;
        private const string CacheKeyPrefix = "ProxyData_";
        private static readonly string Destination = "upstream";

        private readonly List<string> discardTypes = ["infonpc"];
        private readonly List<string> logTypes = ["user_input", "inputtext", "chat", "_speech"];

        private readonly CacheTools cacheTools;

        public ConditionalProxyController(
            IHttpForwarder httpForwarder,
            IMemoryCache memoryCache,
            HttpMessageInvoker httpClient)
        {
            _httpForwarder = httpForwarder;
            _memoryCache = memoryCache;
            _httpClient = httpClient;

            cacheTools = new CacheTools(_memoryCache);
        }

        [HttpGet("comm.php")]
        public async Task<IActionResult> Get([FromQuery] string DATA)
        {
            if (string.IsNullOrEmpty(DATA))
            {
                return BadRequest("DATA parameter is required.");
            }

            try
            {
                // Decode base64 DATA parameter
                byte[] dataBytes = Convert.FromBase64String(DATA);
                string decodedData = Encoding.UTF8.GetString(dataBytes);

                var gameRequest = decodedData.Split('|', 4);

                _ = cacheTools.HitCount("total", true); // total requests

                // check if the request is one of the ones we might discard
                if (discardTypes.Contains(gameRequest[0]))
                {
                    // check if the data is different from the cache
                    if (!IsNewInfo(gameRequest[0], gameRequest[3]))
                    {
                        //Console.ForegroundColor = ConsoleColor.DarkYellow;
                        //Console.WriteLine(DateTime.Now.ToLongTimeString() + " Duplicate {0}. Terminating. ({1:000000})", gameRequest[0], count);
                        //Console.ResetColor();

                        _ = cacheTools.HitCount("dropped", true); // dropped requests
                        Response.ContentType = "text/html; charset=UTF-8";
                        return Ok();
                    }
                    else
                    {
                        _memoryCache.Set(CacheKeyPrefix + gameRequest[0], gameRequest[3], TimeSpan.FromSeconds(10)); // 10 second cache

                        // Proxy the request using YARP
                        //Console.WriteLine(DateTime.Now.ToLongTimeString() + " {0} Length: {1:0000}  ({2:000000})", gameRequest[0], gameRequest[3].Length, count);
                        return await ProxyRequest(HttpContext, DATA);
                    }
                }

                /*
                if (logTypes.Contains(gameRequest[0]))
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine(DateTime.Now.ToLongTimeString() + " {0} Length: {1:0000}  ({2:000000}) -- {3}", gameRequest[0], gameRequest[3].Length, count, gameRequest[3]);
                    Console.ResetColor();
                }
                 else
                */
                    //Console.WriteLine(DateTime.Now.ToLongTimeString() + " {0} Length: {1:0000}  ({2:000000})", gameRequest[0], gameRequest[3].Length, count);
                // Proxy the request using YARP
                return await ProxyRequest(HttpContext, DATA);
            }
            catch (FormatException)
            {
                //return BadRequest("Invalid base64 string in DATA parameter.");

                // don't care if it's bad. send it anyways
                return await ProxyRequest(HttpContext, DATA);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet]
        [HttpPost]
        [HttpPut]
        public async Task<IActionResult> ProxyPostPut()
        {
            // Always proxy POST and PUT requests
            return await ProxyRequest(HttpContext, null);
        }

        private async Task<IActionResult> ProxyRequest(HttpContext context, string? data)
        {
            var transformer = HttpTransformer.Default;
            if (data != null)
            {
                transformer = new CustomQueryTransformer(data);
            }

            var error = await _httpForwarder.SendAsync(context, Destination, _httpClient, ForwarderRequestConfig.Empty, transformer);

            if (error != ForwarderError.None)
            {
                var errorFeature = context.GetForwarderErrorFeature();
                if (errorFeature != null)
                {
                    return StatusCode(500, $"Proxy error: {errorFeature.Exception?.Message}");
                }
            }

            // Response is already written by YARP
            return new EmptyResult();
        }

        private class CustomQueryTransformer : HttpTransformer
        {
            private readonly string _data;
            private const string UpstreamBaseUrl = "http://127.0.0.1:8081/HerikaServer/";

            public CustomQueryTransformer(string data)
            {
                _data = data;
            }

            public override async ValueTask TransformRequestAsync(HttpContext httpContext, HttpRequestMessage proxyRequest, string destinationPrefix)
            {
                // Let YARP handle the base request transformation
                await base.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix);

                proxyRequest.RequestUri = new Uri($"{UpstreamBaseUrl}comm.php?DATA={_data}");
                //Console.WriteLine($"Constructed URI: {proxyRequest.RequestUri}");
            }
        }



        /// <summary>
        /// Check to see if the provided info is the same as the last or not.
        /// </summary>
        /// <param name="infoType"></param>
        /// <param name="infoValue"></param>
        /// <returns></returns>
        private bool IsNewInfo(string infoType, string? infoValue)
        {
            var lastinfo = GetLastInfo(infoType);

            if (string.IsNullOrEmpty(infoValue)) return true;

            return !infoValue.Equals(lastinfo);
        }

        /// <summary>
        /// Get the cached value for a key
        /// </summary>
        /// <param name="infoType"></param>
        /// <returns></returns>
        private string? GetLastInfo(string infoType)
        {
            if (_memoryCache.TryGetValue(CacheKeyPrefix + infoType, out string? cachedResponse))
            {
                return cachedResponse;
            }
            return null;
        }
    }
}