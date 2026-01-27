using EPR.PRN.ObligationCalculation.Function.Extensions;
using EPR.PRN.ObligationCalculation.Function.Handlers;
using EPR.PRN.ObligationCalculation.Function.Services;
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
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddApplicationInsights();
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                .ConfigureServices((hostingContext, services) =>
                {
                    services.AddCustomApplicationInsights();
                    services.AddHttpClient();
                    services.AddScoped<IEprCommonDataApiService, EprCommonDataApiService>();
                    services.AddScoped<IEprPrnCommonBackendService, EprPrnCommonBackendService>();
                    services.AddTransient<EprCommonDataApiAuthorisationHandler>();
                    services.AddTransient<EprPrnCommonBackendAuthorisationHandler>();
                    services.ConfigureOptions(hostingContext.Configuration);
                    services.AddHttpClients();
                    services.AddAzureClients(hostingContext.Configuration);
                })
                .Build();

            await host.RunAsync();
        }

        public static IServiceCollection AddCustomApplicationInsights(this IServiceCollection services)
        {
            services.AddApplicationInsightsTelemetryWorkerService(options =>
            {
                options.ConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
            });

            services.ConfigureFunctionsApplicationInsights();

            services.Configure<LoggerFilterOptions>(options =>
            {
                const string aiProvider = "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider";
                var defaultRule = options.Rules.FirstOrDefault(r => r.ProviderName == aiProvider);
                if (defaultRule != null) options.Rules.Remove(defaultRule);
                options.Rules.Add(new LoggerFilterRule(aiProvider, null, LogLevel.Information, null));
            });

            return services;
        }
    }
}
