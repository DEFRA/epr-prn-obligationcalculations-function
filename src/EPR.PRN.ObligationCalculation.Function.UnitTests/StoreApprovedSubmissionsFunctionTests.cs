#nullable disable

using AutoFixture;
using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.DTOs;
using EPR.PRN.ObligationCalculation.Application.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace EPR.PRN.ObligationCalculation.Function.Tests;

[TestClass()]
public class StoreApprovedSubmissionsFunctionTests
{
    private Fixture _fixture;
    private Mock<ISubmissionsDataService> _submissionsDataService;
    private Mock<ILogger<StoreApprovedSubmissionsFunction>> _loggerMock;
    private Mock<IServiceBusProvider> _serviceBusProviderMock;
    private Mock<IOptions<ApplicationConfig>> _configMock;
    private StoreApprovedSubmissionsFunction _function;
    private TimerInfo _timerInfo;

    [TestInitialize]
    public void TestInitialize()
    {
        _fixture = new Fixture();
        _timerInfo = new TimerInfo();
        _loggerMock = new Mock<ILogger<StoreApprovedSubmissionsFunction>>();
        _submissionsDataService = new Mock<ISubmissionsDataService>();
        _serviceBusProviderMock = new Mock<IServiceBusProvider>();

        _configMock = new Mock<IOptions<ApplicationConfig>>();
        var config = new ApplicationConfig
        {
            UseDefaultRunDate = false,
            DefaultRunDate = "2024-01-01"
        };

        _configMock.Setup(c => c.Value).Returns(config);
    }

    [TestMethod]
    [DataRow(true, "2024-01-01", 3)]
    [DataRow(false, "2024-10-10", 3)]
    public async Task RunAsync_ShouldSendApprovedSubmissionsToQueue_WhenSubmissionsSentToQueue(bool useDefaultRunDate, string lastSuccessfulRunDate, int logInformationCount)
    {
        // Arrange
        _configMock.Object.Value.UseDefaultRunDate = useDefaultRunDate;
        _function = new StoreApprovedSubmissionsFunction(
            _loggerMock.Object,
            _submissionsDataService.Object,
            _serviceBusProviderMock.Object,
            _configMock.Object);

        var currentRunDate = DateTime.Now.Date.ToString();
        _serviceBusProviderMock.Setup(x => x.GetLastSuccessfulRunDateFromQueue()).ReturnsAsync(lastSuccessfulRunDate);
        var approvedSubmissionEntities = _fixture.CreateMany<ApprovedSubmissionEntity>(3).ToList();
        _submissionsDataService
            .Setup(x => x.GetApprovedSubmissionsData(lastSuccessfulRunDate))
            .ReturnsAsync(approvedSubmissionEntities);
        _serviceBusProviderMock
            .Setup(x => x.SendApprovedSubmissionsToQueueAsync(approvedSubmissionEntities))
            .Returns(Task.CompletedTask);
        _serviceBusProviderMock
            .Setup(x => x.SendSuccessfulRunDateToQueue(currentRunDate))
            .Returns(Task.CompletedTask);

        // Act
        await _function.RunAsync(_timerInfo);

        // Assert
        _loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Exactly(logInformationCount));
    }

    [DataRow(true, null)]
    [DataRow(false, null)]
    public async Task RunAsync_Terminated_WhenRunDateIsNullOrEmpty(bool useDefaultRunDate, string lastSuccessfulRunDate)
    {
        // Arrange
        _configMock.Object.Value.UseDefaultRunDate = useDefaultRunDate;
        _function = new StoreApprovedSubmissionsFunction(
            _loggerMock.Object,
            _submissionsDataService.Object,
            _serviceBusProviderMock.Object,
            _configMock.Object);

        _serviceBusProviderMock.Setup(x => x.GetLastSuccessfulRunDateFromQueue()).ReturnsAsync(lastSuccessfulRunDate);

        // Act
        await _function.RunAsync(_timerInfo);

        // Assert
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Exactly(1));

    }


    [TestMethod]
    public async Task RunAsync_ShouldHandleException_WhenErrorOccurs()
    {
        // Arrange
        _function = new StoreApprovedSubmissionsFunction(
            _loggerMock.Object,
            _submissionsDataService.Object,
            _serviceBusProviderMock.Object,
            _configMock.Object);

        var lastSuccessfulRunDate = "2024-10-10";
        _serviceBusProviderMock.Setup(x => x.GetLastSuccessfulRunDateFromQueue()).ReturnsAsync(lastSuccessfulRunDate);

        _submissionsDataService
            .Setup(x => x.GetApprovedSubmissionsData(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Test Exception"));

        // Act
        await _function.RunAsync(_timerInfo);

        // Assert
        // Verify that the error is logged
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }
}