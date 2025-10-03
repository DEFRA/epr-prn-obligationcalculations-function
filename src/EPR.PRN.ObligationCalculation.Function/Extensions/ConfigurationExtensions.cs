using Azure.Identity;
using Azure.Messaging.ServiceBus;
using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.Services;
using EPR.PRN.ObligationCalculation.Function.Handlers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

    public static IServiceCollection AddCustomApplicationInsights(this IServiceCollection services)
    {
        // Add AI worker service with custom options
        services.AddApplicationInsightsTelemetryWorkerService(options =>
        {
            options.ConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
            options.EnableAdaptiveSampling = false; // keep all telemetry
        });

        // Configure Functions-specific AI settings
        services.ConfigureFunctionsApplicationInsights();

        // Customize logging rules for Application Insights
        services.Configure<LoggerFilterOptions>(options =>
        {
            const string aiProvider = "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider";

            // Remove existing default rule for AI provider, if any
            var defaultRule = options.Rules.FirstOrDefault(r => r.ProviderName == aiProvider);
            if (defaultRule != null)
            {
                options.Rules.Remove(defaultRule);
            }

            // Add a new rule to log Information level and above for all categories
            options.Rules.Add(
                new LoggerFilterRule(
                    providerName: aiProvider,
                    categoryName: null,
                    logLevel: LogLevel.Information,
                    filter: null
                )
            );
        });

        return services;
    }

	public static IServiceCollection AddAzureClients(this IServiceCollection services)
    {
        services.AddAzureClients(clientBuilder =>
            {
                clientBuilder.AddClient<ServiceBusClient, ServiceBusClientOptions>(options =>
                {
                    options.TransportType = ServiceBusTransportType.AmqpWebSockets;
                    var sp = services.BuildServiceProvider();
                    var serviceBusConfig = sp.GetRequiredService<IOptions<ServiceBusConfig>>().Value;
                    return new(serviceBusConfig.FullyQualifiedNamespace, new DefaultAzureCredential(), options);
                });
            });

        return services;
    }

    public static IServiceCollection AddHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient<ISubmissionsDataService, SubmissionsDataService>((sp, c) =>
        {
            var config = sp.GetRequiredService<IOptions<SubmissionsServiceApiConfig>>().Value;
            c.BaseAddress = new Uri(config.BaseUrl);
            c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			c.Timeout = TimeSpan.FromSeconds(360);
		})
		.AddHttpMessageHandler<SubmissionsServiceAuthorisationHandler>()
        .AddPolicyHandler(GetRetryPolicy());

        services.AddHttpClient<IPrnService, PrnService>((sp, c) =>
        {
            var config = sp.GetRequiredService<IOptions<PrnServiceApiConfig>>().Value;
            c.BaseAddress = new Uri(config.BaseUrl);
            c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			c.Timeout = TimeSpan.FromSeconds(360);
		})
        .AddHttpMessageHandler<PrnServiceAuthorisationHandler>()
		.AddPolicyHandler(GetRetryPolicy());

        return services;
    }

    private static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy() => HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(3, retryAttempt)));
}