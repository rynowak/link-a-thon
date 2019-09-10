using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApiTemplate
{
    public class Program
    {
#if TIME
        public static async Task Main(string[] args)
        {
            if (!args.Contains("--time"))
            {
                var host = CreateHostBuilder(args).Build();
            
#if CUSTOM_BUILDER
                host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStarted.Register(() =>
                {
                    Console.WriteLine("Application started.");
                });
#endif

                host.Run();
            }
            else
            {
                using (var host = CreateHostBuilder(args).Start())
                {
                    var client = new HttpClient();
                    var response = await client.GetAsync("http://localhost:5000/WeatherForecast");
                    response.EnsureSuccessStatusCode();
                    Console.WriteLine("Completed At: ", DateTime.Now.Ticks);
                }
            }
        }
#else
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            
            #if CUSTOM_BUILDER
                host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStarted.Register(() =>
                {
                    Console.WriteLine("Application started.");
                });
            #endif

            host.Run();
        }
#endif

#if CUSTOM_BUILDER
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseContentRoot(Directory.GetCurrentDirectory());
                    webBuilder.UseKestrel();
                    webBuilder.UseUrls("http://*:5000");
                    webBuilder.UseStartup<Startup>();
                });
#else
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls("http://*:5000");
                    webBuilder.UseStartup<Startup>();
                });
#endif
    }
}
