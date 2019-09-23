using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace PlatformJson
{
    public class Program
    {
        private static ManualResetEventSlim shutdown;

        public static async Task Main(string[] args)
        {
            shutdown = new ManualResetEventSlim();
            Console.CancelKeyPress += Console_CancelKeyPress;

            var options = new KestrelServerOptions();
            options.ListenAnyIP(5000, c =>
            {
                c.UseHttpApplication<Application>();
            });

            var server = new KestrelServer(
                Options.Create(options),
                new SocketTransportFactory(
                    Options.Create(new SocketTransportOptions()),
                    NullLoggerFactory.Instance),
                NullLoggerFactory.Instance);

            var application = new HostingApplication((context) => Task.CompletedTask, new DefaultHttpContextFactory(new ServiceProvider()));
            await server.StartAsync(application, CancellationToken.None);

            Console.WriteLine("Application started.");
            shutdown.Wait();

            Console.WriteLine("Shutting down...");
            server.Dispose();
            await server.StopAsync(CancellationToken.None);
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            shutdown.Set();
        }

        private class ServiceProvider : IServiceProvider
        {
            public object GetService(Type serviceType)
            {
                if (serviceType == typeof(IOptions<FormOptions>))
                {
                    return Options.Create(new FormOptions());
                }

                if (serviceType == typeof(IServiceScopeFactory))
                {
                    return new ServiceScopeFactory();
                }

                return null;
            }
        }

        private class ServiceScopeFactory : IServiceScopeFactory
        {
            public IServiceScope CreateScope()
            {
                return new ServiceScope();
            }
        }

        private class ServiceScope : IServiceScope
        {
            public IServiceProvider ServiceProvider => new ServiceProvider();

            public void Dispose()
            {
            }
        }
    }
}
