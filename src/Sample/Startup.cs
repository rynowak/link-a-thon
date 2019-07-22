using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using uHosting;

namespace Sample
{
    public class Startup: IMicroStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddAuthorization()
                .AddAuthorizationPolicyEvaluator()
                .AddJson();
        }

        public void ConfigureMicroServices(MicroContainerBuilder services)
        {
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapController<MyController>();
            });
        }
    }
}