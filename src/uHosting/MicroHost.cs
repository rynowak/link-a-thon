using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace uHosting
{
    public class MicroHost
    {
        private MicroHostOptions _uHostOptions;
        private Func<IMicroStartup> _startupFactory;

        public MicroHost(MicroHostOptions uHostOptions, Func<IMicroStartup> startupFactory)
        {
            _uHostOptions = uHostOptions;
            _startupFactory = startupFactory;
        }

        public static MicroHostBuilder CreateDefaultBuilder(string[] args)
        {
            var dump = args.Any(a => string.Equals("[dump]", a, StringComparison.OrdinalIgnoreCase));
            return new MicroHostBuilder(new MicroHostOptions()
            {
                DumpMode = dump
            });
        }

        public int Run()
        {
            // Activate the startup type
            var startup = _startupFactory();

            if(_uHostOptions.DumpMode)
            {
                DumpMode(startup);
            }
            else
            {
                YeetMode();
            }
            return 0;
        }

        private void YeetMode()
        {
            Console.WriteLine("Running MicroHost app...");
        }

        private void DumpMode(IMicroStartup startup)
        {
            var services = new ServiceCollection();
            startup.ConfigureServices(services);

            var writer = new StringWriter();
            CodeGenerator.GenerateFactories(writer, services);
            Console.WriteLine(writer.ToString());
        }
    }
}
