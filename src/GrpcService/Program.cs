using System;
using System.Collections.Generic;
using System.IO;
#if TIME
using System.Diagnostics;
using Grpc.Net.Client;
using Grpc.Testing;
using Google.Protobuf;
#endif
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace GrpcService
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
                    AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                    var channel = GrpcChannel.ForAddress("http://localhost:5000");
                    var client = new BenchmarkService.BenchmarkServiceClient(channel);

                    var request = new SimpleRequest
                    {
                        Payload = new Payload { Body = ByteString.CopyFrom(new byte[0]) },
                        ResponseSize = 0
                    };
                    _ = await client.UnaryCallAsync(request);
                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;
                    Console.WriteLine("Stopwatch startup measurement: " + ts.Milliseconds);
                }
                return;
            }
            else
            {
                var host = CreateHostBuilder(args).Build();

                host.Run();
            }
        }
#else
        public static void Main(string[] args)
        {
            // The more accurate numbers are measured using the aspnet
            // benchmarking infrastructure to make requests from
            // another server.
            var host = CreateHostBuilder(args).Build();

            host.Run();
        }
#endif

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(kestrelOptions =>
                    {
                        kestrelOptions.ConfigureEndpointDefaults(listenOptions =>
                        {
                            listenOptions.Protocols = HttpProtocols.Http2;
                        });
                    });
                    webBuilder.UseUrls("http://*:5000");
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureLogging(loggingBuilder =>
                                  // Don't perturb the measurement with log spew for every request
                                  // Do this in code to avoid shipping a settings file for the single executable
                                  loggingBuilder.SetMinimumLevel(LogLevel.Error)
                                                // But keep logging for app startup/shutdown
                                                .AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information));
    }
}
