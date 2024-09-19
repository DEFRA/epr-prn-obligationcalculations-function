using EPR.PRN.ObligationCalculation.Application.Services;
using EPR.PRN.ObligationCalculation.Function.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
        services.AddAzureClients();

    })
    .Build();

await host.RunAsync();
