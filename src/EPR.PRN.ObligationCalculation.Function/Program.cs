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
                    services.AddApplicationInsightsTelemetryWorkerService(options =>
                    {
                        options.ConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
                        options.EnableAdaptiveSampling = false;
                    });
                    services.ConfigureFunctionsApplicationInsights();
                    services.Configure<LoggerFilterOptions>(options =>
                    {
                        const string aiProvider =
                            "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider";

                        var defaultRule = options.Rules.FirstOrDefault(r => r.ProviderName == aiProvider);
                        if (defaultRule is not null)
                        {
                            options.Rules.Remove(defaultRule);
                        }

                        options.Rules.Add(new LoggerFilterRule(
                            providerName: aiProvider,
                            categoryName: null,
                            logLevel: LogLevel.Information,
                            filter: null));
                    });
            
                    services.AddHttpClient();
                    services.AddScoped<ISubmissionsDataService, SubmissionsDataService>();
                    services.AddScoped<IPrnService, PrnService>();
                    services.AddScoped<IServiceBusProvider, ServiceBusProvider>();
			        services.AddTransient<PrnServiceAuthorisationHandler>();
			        services.AddTransient<SubmissionsServiceAuthorisationHandler>();
			        services.ConfigureOptions(hostingContext.Configuration);
                    services.AddHttpClients();
                    services.AddAzureClients();
                })
                .Build();

            await host.RunAsync();
        }
    }
}