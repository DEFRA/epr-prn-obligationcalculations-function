using Azure.Identity;
using Azure.Messaging.ServiceBus;
using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Function.Handlers;
using EPR.PRN.ObligationCalculation.Function.Services;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;

namespace EPR.PRN.ObligationCalculation.Function.Extensions;

[ExcludeFromCodeCoverage]
public static class ConfigurationExtensions
{
    public static IServiceCollection ConfigureOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ServiceBusConfig>(configuration.GetSection(ServiceBusConfig.SectionName));
        services.Configure<SubmissionsServiceApiConfig>(configuration.GetSection(SubmissionsServiceApiConfig.SectionName));
        services.Configure<PrnServiceApiConfig>(configuration.GetSection(PrnServiceApiConfig.SectionName));
        services.Configure<ApplicationConfig>(configuration.GetSection(ApplicationConfig.SectionName));
        return services;
    }

    public static IServiceCollection AddAzureClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAzureClients(clientBuilder =>
            {
                clientBuilder.AddClient<ServiceBusClient, ServiceBusClientOptions>(options =>
                {
                    var serviceBusConfig = services.BuildServiceProvider().GetRequiredService<IOptions<ServiceBusConfig>>().Value;

                    if (!string.IsNullOrWhiteSpace(serviceBusConfig.FullyQualifiedNamespace))
                    {
                        options.TransportType = ServiceBusTransportType.AmqpWebSockets;
                        return new ServiceBusClient(fullyQualifiedNamespace: serviceBusConfig.FullyQualifiedNamespace, new DefaultAzureCredential(), options);
                    }
                    else
                    {
                        return new ServiceBusClient(connectionString: configuration["ServiceBus"], options);
                    }
                });
            });

        return services;
    }

    public static IServiceCollection AddHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient<IEprCommonDataApiService, EprCommonDataApiService>((sp, c) =>
        {
            var config = sp.GetRequiredService<IOptions<SubmissionsServiceApiConfig>>().Value;
            c.BaseAddress = new Uri(config.BaseUrl);
            c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			c.Timeout = TimeSpan.FromSeconds(360);
		})
		.AddHttpMessageHandler<EprCommonDataApiAuthorisationHandler>()
        .AddPolicyHandler(GetRetryPolicy());

        services.AddHttpClient<IEprPrnCommonBackendService, EprPrnCommonBackendService>((sp, c) =>
        {
            var config = sp.GetRequiredService<IOptions<PrnServiceApiConfig>>().Value;
            c.BaseAddress = new Uri(config.BaseUrl);
            c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			c.Timeout = TimeSpan.FromSeconds(360);
		})
        .AddHttpMessageHandler<EprPrnCommonBackendAuthorisationHandler>()
		.AddPolicyHandler(GetRetryPolicy());

        return services;
    }

    private static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy() => HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(r => (int)r.StatusCode == 499)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(3, retryAttempt)));
}