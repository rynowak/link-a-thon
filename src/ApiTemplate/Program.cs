using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApiTemplate
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            if (!args.Contains("--time"))
            {
                CreateHostBuilder(args).Build().Run();
                return;
            }

            using (var host = CreateHostBuilder(args).Start())
            {
                var client = new HttpClient();
                var response = await client.GetAsync("https://localhost:5001/WeatherForecast");
                response.EnsureSuccessStatusCode();
                Console.WriteLine("Completed At: ", DateTime.Now.Ticks);
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
