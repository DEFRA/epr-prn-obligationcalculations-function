using EPR.PRN.ObligationCalculation.Application.Services;
using EPR.PRN.ObligationCalculation.Function.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            services.AddApplicationInsightsTelemetryWorkerService();
            services.ConfigureFunctionsApplicationInsights();
            services.AddHttpClient();
            services.AddScoped<ISubmissionsDataService, SubmissionsDataService>();
            services.AddScoped<IAppInsightsProvider, AppInsightsProvider>();
            services.AddScoped<IServiceBusProvider, ServiceBusProvider>();
            services.ConfigureOptions(hostingContext.Configuration);
            services.AddHttpClients();
            services.AddAzureClients(hostingContext.Configuration);

        })
        .Build();

            await host.RunAsync();
        }
    }
}