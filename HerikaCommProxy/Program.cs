using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Configuration;

namespace HerikaCommProxy
{
    public class Program
    {
        public static async Task Main(string[] args)
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

            await CheckVersionAsync();


            app.Run();
        }

        private static async Task CheckVersionAsync()
        {
            var checker = new GitHubVersionChecker();

            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string path = assembly.Location;
                if (string.IsNullOrWhiteSpace(path))
                {
                    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                        path = "HerikaCommProxy.exe";
                    else
                        path = "HerikaCommProxy";
                }
                FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(path);
                string currentVersion = fileVersionInfo.ProductVersion;

                var (updateAvailable, latestVersion) = await checker.CheckForUpdateAsync("vrelk", "HerikaCommProxy", currentVersion);

                if (!updateAvailable)
                {
                    string message = "https://github.com/vrelk/HerikaCommProxy/releases";
                    
                    latestVersion = $"Update available! Latest version: {latestVersion}";
                    currentVersion = $"Current version: {currentVersion}";
                    int width = Console.WindowWidth - 1;

                    Console.ForegroundColor = ConsoleColor.White;
                    Console.BackgroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine(new string(' ', width));
                    Console.WriteLine(new string(' ', width));
                    Console.WriteLine(latestVersion + new string(' ', width - latestVersion.Length));
                    Console.WriteLine(currentVersion + new string(' ', width - currentVersion.Length));
                    Console.WriteLine(new string(' ', width));
                    Console.WriteLine(message + new string(' ', width - message.Length));
                    Console.WriteLine(new string(' ', width));
                    Console.WriteLine(new string(' ', width));
                    Console.ResetColor();
                    Console.Write("\n\n\n");
                }
                else
                {
                    Console.BackgroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine("                            ");
                    Console.WriteLine("                            ");
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("Your software is up to date.");
                    Console.BackgroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine("                            ");
                    Console.WriteLine("                            ");
                    Console.ResetColor();
                    Console.Write("\n\n\n");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking for updates: {ex.Message}");
            }

        }
    }
}
