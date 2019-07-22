using System;

namespace uHosting
{
    public class MicroHostBuilder
    {
        private readonly MicroHostOptions _uHostOptions;
        private Func<IMicroStartup> _startupFactory;

        public MicroHostBuilder(MicroHostOptions uHostOptions)
        {
            _uHostOptions = uHostOptions;
        }

        public MicroHostBuilder UseStartup<T>() where T: IMicroStartup, new()
        {
            _startupFactory = () => new T();
            return this;
        }

        public MicroHostBuilder UseStartup<T>(Func<IMicroStartup> factory) where T: IMicroStartup
        {
            _startupFactory = factory;
            return this;
        }

        public MicroHost Build()
        {
            return new MicroHost(_uHostOptions, _startupFactory);
        }
    }
}