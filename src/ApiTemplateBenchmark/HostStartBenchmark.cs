using System;
using System.Reflection;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace ApiTemplateBenchmarks
{
    public class HostStartBenchmark
    {
        private const int Unroll = 25;

        private Microsoft.Extensions.Hosting.IHost[] _hosts;

        public HostStartBenchmark()
        {
            _hosts = new Microsoft.Extensions.Hosting.IHost[Unroll];
        }

        [IterationCleanup]
        public void Shutdown()
        {
            for (var i = 0; i < Unroll; i++)
            {
                if (_hosts[i] != null)
                {
                    _hosts[i].Dispose();
                    _hosts[i].StopAsync().GetAwaiter().GetResult();
                    _hosts[i] = null;
                }
            }
        }

        [Benchmark(OperationsPerInvoke = Unroll)]
        public async Task CreateAndStart()
        {
            for (var i = 0; i < Unroll; i++)
            {
                var args = new [] { "--urls", $"http://localhost:{5000 + i}" };
                var builder = ApiTemplate.Program.CreateHostBuilder(args);
                builder.ConfigureAppConfiguration((context, _) =>
                {
                    context.HostingEnvironment.ApplicationName = new AssemblyName(typeof(ApiTemplate.Startup).Assembly.FullName).Name;
                });
                _hosts[i] = builder.Build();
                await _hosts[i].StartAsync();
            }
        }
    }
}