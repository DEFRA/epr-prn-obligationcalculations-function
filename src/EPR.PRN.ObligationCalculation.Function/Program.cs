using EPR.PRN.ObligationCalculation.Application;
using EPR.PRN.ObligationCalculation.Application.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddHttpClient();
        services.AddScoped<ISubmissionsDataService, SubmissionsDataService>();
        services.AddScoped<IAppInsightsProvider, AppInsightsProvider>();
    })
    .Build();

host.Run();
