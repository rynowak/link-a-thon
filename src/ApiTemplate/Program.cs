using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
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
            // For local measurements, use a stopwatch and make a
            // request from the app itself to get an approximate
            // measurement.
            if (args.Contains("--time"))
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                using (var host = CreateHostBuilder(args).Start())
                {
                    var client = new HttpClient();
                    var response = await client.GetAsync("http://localhost:5000/WeatherForecast");
                    response.EnsureSuccessStatusCode();
                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;
                    Console.WriteLine("Stopwatch startup measurement: " + ts.Milliseconds);
                }
                return;
            }

            var host = CreateHostBuilder(args).Build();

#if CUSTOM_BUILDER
            host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStarted.Register(() =>
            {
                Console.WriteLine("Application started.");
            });
#endif
            host.Run();
        }

#else
        public static void Main(string[] args)
        {
            // The more accurate numbers are measured using the aspnet
            // benchmarking infrastructure to make requests from
            // another server.
            var host = CreateHostBuilder(args).Build();

#if CUSTOM_BUILDER
            // The CUSTOM_BUILDER configuration removes logging, so we need to manually tell the server when 
            // we're ready for traffic.
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
                    webBuilder.UseStartup<StartupWithoutMvc>();
                });
#else
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls("http://*:5000");
    #if NO_MVC
                    webBuilder.UseStartup<StartupWithoutMvc>();
    #else
                    webBuilder.UseStartup<Startup>();
    #endif
                })
                .ConfigureLogging(loggingBuilder =>
                                  // Don't perturb the measurement with log spew for every request
                                  // Do this in code to avoid shipping a settings file for the single executable
                                  loggingBuilder.SetMinimumLevel(LogLevel.Error)
                                                // But keep logging for app startup/shutdown
                                                .AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information));
#endif
    }
}
