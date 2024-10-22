namespace EPR.PRN.ObligationCalculation.Function.Extensions;

using Azure.Core;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.Services;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;

[ExcludeFromCodeCoverage]
public static class ConfigurationExtensions
{
    public static IServiceCollection ConfigureOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ServiceBusConfig>(configuration.GetSection(ServiceBusConfig.SectionName));
        services.Configure<CommonDataApiConfig>(configuration.GetSection(CommonDataApiConfig.SectionName));
        services.Configure<CommonBackendApiConfig>(configuration.GetSection(CommonBackendApiConfig.SectionName));
        return services;
    }

    public static IServiceCollection AddAzureClients(this IServiceCollection services, IConfiguration configuration)
    {
        var isDevMode = configuration.GetValue<bool?>("ApiConfig:DeveloperMode");
        if (isDevMode is true)
        {
            services.AddAzureClients(clientBuilder =>
            {
                clientBuilder.AddClient<ServiceBusClient, ServiceBusClientOptions>(options =>
                {
                    options.TransportType = ServiceBusTransportType.AmqpWebSockets;
                    var sp = services.BuildServiceProvider();
                    var serviceBusConfig = sp.GetRequiredService<IOptions<ServiceBusConfig>>().Value;
                    return new(serviceBusConfig.ConnectionString, options);
                });
            });
        }
        else
        {
            services.AddAzureClients(clientBuilder =>
            {
                clientBuilder.AddClient<ServiceBusClient, ServiceBusClientOptions>(options =>
                {
                    options.TransportType = ServiceBusTransportType.AmqpWebSockets;
                    var sp = services.BuildServiceProvider();
                    var serviceBusConfig = sp.GetRequiredService<IOptions<ServiceBusConfig>>().Value;
                    return new(serviceBusConfig.Namespace, new DefaultAzureCredential(), options);
                });
            });
        }

        return services;
    }

    public static IServiceCollection AddHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient<ISubmissionsDataService, SubmissionsDataService>((sp, c) =>
        {
            var config = sp.GetRequiredService<IOptions<CommonDataApiConfig>>().Value;
            c.BaseAddress = new Uri(config.BaseUrl);
            c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        services.AddHttpClient<IPrnService, PrnService>((sp, c) =>
        {
            var config = sp.GetRequiredService<IOptions<CommonBackendApiConfig>>().Value;
            c.BaseAddress = new Uri(config.BaseUrl);
            c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        return services;
    }
}