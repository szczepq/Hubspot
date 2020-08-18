using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SoftwareHut.HubspotService.Filters
{
    public class ConfigurationValidationStartupFilter<TConfigurationClass> : IStartupFilter
        where TConfigurationClass : class
    {
        public IServiceProvider ServiceProvider { get; }

        public ConfigurationValidationStartupFilter(
            IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public Action<IApplicationBuilder> Configure(
            Action<IApplicationBuilder> next)
        {
            try
            {
                ServiceProvider.GetService(typeof(TConfigurationClass));
            }
            catch (OptionsValidationException ex)
            {
                throw;
            }
            return next;
        }
    }
}