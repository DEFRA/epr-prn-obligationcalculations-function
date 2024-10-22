#nullable disable

using AutoFixture;
using EPR.PRN.ObligationCalculation.Application.DTOs;
using EPR.PRN.ObligationCalculation.Application.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;

namespace EPR.PRN.ObligationCalculation.Function.Tests;

[TestClass()]
public class StoreApprovedSubmissionsFunctionTests
{
    private Fixture _fixture;
    private Mock<ISubmissionsDataService> _submissionsDataService;
    private Mock<ILogger<StoreApprovedSubmissionsFunction>> _loggerMock;
    private Mock<IServiceBusProvider> _serviceBusProviderMock;
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
        _function = new StoreApprovedSubmissionsFunction(
            _loggerMock.Object,
            _submissionsDataService.Object,
            _serviceBusProviderMock.Object);
    }

    [TestMethod()]
    public async Task RunAsync_ShouldSendApprovedSubmissionsToQueue_WhenSubmissionsSentToQueue()
    {
        // Arrange
        var approvedSubmissionEntities = _fixture.CreateMany<ApprovedSubmissionEntity>(3).ToList();
        _submissionsDataService
            .Setup(x => x.GetApprovedSubmissionsData(It.IsAny<string>()))
            .ReturnsAsync(approvedSubmissionEntities);
        _serviceBusProviderMock
            .Setup(x => x.SendApprovedSubmissionsToQueueAsync(approvedSubmissionEntities))
            .Returns(Task.CompletedTask);
        // Act
        await _function.RunAsync(_timerInfo);

        // Assert
        _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Exactly(2));
        _serviceBusProviderMock.Verify(x => x.SendApprovedSubmissionsToQueueAsync(approvedSubmissionEntities), Times.Once);
    }

    [TestMethod]
    public async Task RunAsync_ShouldHandleException_WhenErrorOccurs()
    {
        // Arrange
        _submissionsDataService
            .Setup(x => x.GetApprovedSubmissionsData(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Test Exception"));

        // Act
        await _function.RunAsync(_timerInfo);

        // Assert
        // Verify that the error is logged
        _loggerMock.Verify(log => log.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Ended with error while storing approved submission")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }
}