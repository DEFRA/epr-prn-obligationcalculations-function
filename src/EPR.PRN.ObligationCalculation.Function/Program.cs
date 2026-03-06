using EPR.PRN.ObligationCalculation.Application.Services;
using EPR.PRN.ObligationCalculation.Function.Extensions;
using EPR.PRN.ObligationCalculation.Function.Handlers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using EPR.PRN.ObligationCalculation.Function.Middleware;
using Microsoft.Extensions.Configuration;

namespace EPR.PRN.ObligationCalculation.Function
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWebApplication(
                    (context, builder) =>
                    {
                        if (IsRunningLocally(context.Configuration))
                            builder.UseMiddleware<FunctionRunningMiddleware>();
                    })
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

                    var runningLocally = IsRunningLocally(hostingContext.Configuration);
                    
                    services.AddAzureClients(hostingContext.Configuration, runningLocally);

                    if (runningLocally)
                    {
                        services.AddTransient<IBlobStorage, BlobStorage>();
                        services.AddTransient<FunctionRunningMiddleware>();
                    }
                })
                .ConfigureLogging(logging =>
                {
                    logging.Services.Configure<LoggerFilterOptions>(options =>
                    {
                        /*
                         * By default, logs with LogLevel.Warning or higher are sent to Application Insights.
                         * To change this, remove the default rule so other log levels are sent to Application Insights.
                         * See for more information: https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide?tabs=hostbuilder%2Cwindows#managing-log-levels
                         * The default log level for Azure Functions is Information. So by removing the default rule, Information and above will be sent to Application Insights.
                         */
                        var defaultRule = options.Rules.FirstOrDefault(rule =>
                            rule.ProviderName ==
                            "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");

                        if (defaultRule is not null)
                        {
                            options.Rules.Remove(defaultRule);
                        }
                    });
                })
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    var hostRoot = "";
                    if (hostContext.HostingEnvironment.IsProduction())
                        hostRoot = "/home/site/wwwroot/";

                    config.AddJsonFile($"{hostRoot}host.json", optional: false);
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                })
                .Build();

            await host.RunAsync();
        }

        private static IServiceCollection AddCustomApplicationInsights(this IServiceCollection services)
        {
            services.AddApplicationInsightsTelemetryWorkerService(options =>
            {
                options.ConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
            });

            services.ConfigureFunctionsApplicationInsights();

            return services;
        }
        
        private static bool IsRunningLocally(IConfiguration configuration) =>
            configuration.GetValue<bool?>("IsRunningLocally") ?? false;
    }
}
