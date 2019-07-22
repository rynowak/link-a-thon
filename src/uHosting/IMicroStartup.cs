using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace uHosting
{
    public interface IMicroStartup
    {
        public abstract void ConfigureServices(IServiceCollection services);
        public abstract void ConfigureMicroServices(MicroContainerBuilder services);
        public abstract void Configure(IApplicationBuilder app, IWebHostEnvironment env);
    }
}
