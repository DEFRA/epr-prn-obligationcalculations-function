#nullable disable

using EPR.PRN.ObligationCalculation.Application.DTOs;
using EPR.PRN.ObligationCalculation.Application.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics.CodeAnalysis;

namespace EPR.PRN.ObligationCalculation.Function.Tests;

[ExcludeFromCodeCoverage]
[TestClass()]
public class StoreApprovedSubmissionsFunctionTests
{
    Mock<ISubmissionsDataService> submissionsDataService;
    Mock<ILogger<StoreApprovedSubmissionsFunction>> logger;
    Mock<IConfiguration> configuration;
    Mock<IAppInsightsProvider> appInsightsProvider;
    Mock<IServiceBusProvider> serviceBusProvider;
    Mock<StoreApprovedSubmissionsFunction> storeApprovedSubmissionsFunction;

    DateTime approvedAfterDate;
    List<ApprovedSubmissionEntity> approvedSubmissionEntities;

    [TestInitialize]
    public void TestInitialize()
    {
        approvedAfterDate = Convert.ToDateTime("2024-01-01");
        approvedSubmissionEntities = new List<ApprovedSubmissionEntity>
        {
            new ApprovedSubmissionEntity {
                SubmissionId = Guid.Empty,
                PackagingMaterial = "PC",
                PackagingMaterialWeight = 100,
                SubmissionPeriod = "2024 P1",
                OrganisationId = 1234
            },
            new ApprovedSubmissionEntity {
                SubmissionId = Guid.Empty,
                PackagingMaterial = "PL",
                PackagingMaterialWeight = 200,
                SubmissionPeriod = "2024 P2",
                OrganisationId = 1234
            }
        };

        logger = new Mock<ILogger<StoreApprovedSubmissionsFunction>>();
        configuration = new Mock<IConfiguration>();

        appInsightsProvider = new Mock<IAppInsightsProvider>();
        appInsightsProvider.Setup(x => x.GetParameterForApprovedSubmissionsApiCall()).ReturnsAsync(approvedAfterDate);

        submissionsDataService = new Mock<ISubmissionsDataService>();
        submissionsDataService.Setup(x => x.GetApprovedSubmissionsData(approvedAfterDate.ToString("yyyy-MM-dd"))).ReturnsAsync(approvedSubmissionEntities);

        serviceBusProvider = new Mock<IServiceBusProvider>();
        serviceBusProvider.Setup(x => x.SendApprovedSubmissionsToQueue(approvedSubmissionEntities)).Returns(Task.CompletedTask);
    }

    [TestMethod()]
    public async Task RunAsyncTest()
    {
        // Arrange
        storeApprovedSubmissionsFunction = new Mock<StoreApprovedSubmissionsFunction>(logger.Object, configuration.Object,
            submissionsDataService.Object, appInsightsProvider.Object, serviceBusProvider.Object);
        var function = storeApprovedSubmissionsFunction.Object;

        TimerInfo timerInfo = new();
        timerInfo.IsPastDue = true;
        timerInfo.ScheduleStatus = new ScheduleStatus
        {
            Last = DateTime.MinValue,
            Next = DateTime.MinValue,
            LastUpdated = DateTime.MinValue
        };

        // Act
        await function.RunAsync(timerInfo);

        // Assert
        serviceBusProvider.Verify(x => x.SendApprovedSubmissionsToQueue(approvedSubmissionEntities), Times.Once);
    }
}