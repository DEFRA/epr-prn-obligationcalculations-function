#nullable disable

using EPR.PRN.ObligationCalculation.Application.DTOs;
using EPR.PRN.ObligationCalculation.Application.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics.CodeAnalysis;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
    StoreApprovedSubmissionsFunction storeApprovedSubmissionsFunction;

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

        storeApprovedSubmissionsFunction = new StoreApprovedSubmissionsFunction(logger.Object, configuration.Object,
            submissionsDataService.Object, appInsightsProvider.Object, serviceBusProvider.Object);
    }

    [TestMethod()]
    public async Task RunAsync_ShouldSendApprovedSubmissionsToQueue_WhenSubmissionsAreAvailable()
    {
        // Arrange
        TimerInfo timerInfo = new();
        timerInfo.IsPastDue = true;
        timerInfo.ScheduleStatus = new ScheduleStatus
        {
            Last = DateTime.MinValue,
            Next = DateTime.MinValue,
            LastUpdated = DateTime.MinValue
        };

        // Act
        await storeApprovedSubmissionsFunction.RunAsync(timerInfo);

        // Assert
        serviceBusProvider.Verify(x => x.SendApprovedSubmissionsToQueue(approvedSubmissionEntities), Times.Once);
    }

    [TestMethod]
    public async Task RunAsync_ShouldLogNoSubmissions_WhenNoSubmissionsAvailable()
    {
        // Arrange
        var timerInfo = new TimerInfo();
        var createdDate = DateTime.Now.AddDays(-1);
        appInsightsProvider
            .Setup(x => x.GetParameterForApprovedSubmissionsApiCall())
            .ReturnsAsync(createdDate);
        var approvedSubmissions = new List<ApprovedSubmissionEntity>();
        submissionsDataService
            .Setup(x => x.GetApprovedSubmissionsData(It.IsAny<string>()))
            .ReturnsAsync(approvedSubmissions);
        // Act
        await storeApprovedSubmissionsFunction.RunAsync(timerInfo);

        // Assert
        logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Exactly(2));

        serviceBusProvider.Verify(x => x.SendApprovedSubmissionsToQueue(It.IsAny<List<ApprovedSubmissionEntity>>()), Times.Never);
    }
}