using EPR.PRN.ObligationCalculation.Application.Services;
using EPR.PRN.ObligationCalculation.Function.Extensions;
using EPR.PRN.ObligationCalculation.Function.Handlers;
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
			services.AddCustomApplicationInsights();
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