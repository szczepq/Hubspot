using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SoftwareHut.HubspotService.Filters;

namespace SoftwareHut.HubspotService.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddConfiguration<TConfigurationInterface, TConfigurationClass>(
            this IServiceCollection serviceCollection,
            IConfiguration configuration)
            where TConfigurationInterface : class
            where TConfigurationClass : class, TConfigurationInterface, new()
        {
            serviceCollection.Configure<TConfigurationClass>(configuration);
            OptionsBuilder<TConfigurationClass> optionsBuilder = serviceCollection.AddOptions<TConfigurationClass>();
            optionsBuilder.ValidateDataAnnotations();
            
            serviceCollection.AddTransient(sp => sp.GetRequiredService<IOptions<TConfigurationClass>>().Value);
            serviceCollection.AddTransient<IStartupFilter, ConfigurationValidationStartupFilter<TConfigurationClass>>();
            serviceCollection.AddTransient(sp => (TConfigurationInterface)sp.GetRequiredService<TConfigurationClass>());
            
            return serviceCollection;
        }
    }
}