using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Configuration;

namespace HerikaCommProxy
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                serverOptions.Listen(System.Net.IPAddress.Parse("0.0.0.0"), 5154);
            });

            // Add services to the container.

            builder.Services.AddControllers();

            // Add YARP services
            builder.Services.AddReverseProxy()
                .LoadFromMemory(new[]
                {
                    new RouteConfig
                    {
                        RouteId = "comm-route",
                        ClusterId = "comm-cluster",
                        Match = new RouteMatch { Path = "/HerikaServer/{**catch-all}" }
                    }
                            }, new[]
                            {
                    new ClusterConfig
                    {
                        ClusterId = "comm-cluster",
                        Destinations = new Dictionary<string, DestinationConfig>
                        {
                            { "upstream", new DestinationConfig { Address = "http://127.0.0.1:8081/" } }
                        }
                    }
                });

            // Add memory cache
            builder.Services.AddMemoryCache();

            // Start the stats task to print to the console...
            builder.Services.AddHostedService<StatsTask>();

            // Add HttpMessageInvoker for YARP
            builder.Services.AddSingleton(new HttpMessageInvoker(new HttpClientHandler()));

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();

            // Configure the HTTP request pipeline
            app.MapReverseProxy();

            app.MapControllers();

            app.Run();
        }
    }
}
