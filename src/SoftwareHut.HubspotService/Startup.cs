using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Refit;
using SoftwareHut.HubspotService.Clients;
using SoftwareHut.HubspotService.Configurations;
using SoftwareHut.HubspotService.Extensions;
using System;
using SoftwareHut.HubspotService.Mappers;

namespace SoftwareHut.HubspotService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddHealthChecks();
            //services
            services.AddConfiguration<IHubspotConfiguration, HubspotConfiguration>(
                Configuration.GetSection(HubspotConfiguration.SectionName));

            // HubspotClient
            var hubspotConfiguration =
                Configuration.GetSection(HubspotConfiguration.SectionName).Get<HubspotConfiguration>();
            services.AddRefitClient<IHubspotClient>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(hubspotConfiguration.BaseUrl));

            // Mappers
            services.AddSingleton<IHubspotMapper, HubspotMapper>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseHealthChecks("/health");
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
