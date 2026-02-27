using EPR.PRN.ObligationCalculation.Application.Services;
using EPR.PRN.ObligationCalculation.Function.Extensions;
using EPR.PRN.ObligationCalculation.Function.Handlers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace EPR.PRN.ObligationCalculation.Function
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var host = new HostBuilder()
        .ConfigureFunctionsWebApplication()
        .ConfigureServices((hostingContext, services) =>
        {
            services.AddCustomApplicationInsights();
            services.AddHttpClient();
            services.AddScoped<ISubmissionsDataService, SubmissionsDataService>();
            services.AddScoped<IPrnService, PrnService>();
            services.AddScoped<IServiceBusProvider, ServiceBusProvider>();
			services.AddTransient<PrnServiceAuthorisationHandler>();
			services.AddTransient<SubmissionsServiceAuthorisationHandler>();
			services.ConfigureOptions(hostingContext.Configuration);
            services.AddHttpClients();
            services.AddAzureClients(hostingContext.Configuration);
        })
        .Build();

            await host.RunAsync();
        }

        public static IServiceCollection AddCustomApplicationInsights(this IServiceCollection services)
        {
            // Add AI worker service with custom options
            services.AddApplicationInsightsTelemetryWorkerService(options =>
            {
                options.ConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
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
    }
}
