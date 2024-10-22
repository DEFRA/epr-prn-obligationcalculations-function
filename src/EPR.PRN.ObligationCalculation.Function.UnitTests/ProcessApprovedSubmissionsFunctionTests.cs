#nullable disable
using Moq;
using Microsoft.Extensions.Logging;
using EPR.PRN.ObligationCalculation.Application.Services;
using Microsoft.Azure.Functions.Worker;

namespace EPR.PRN.ObligationCalculation.Function.UnitTests;

[TestClass]
public class ProcessApprovedSubmissionsFunctionTests
{
    private Mock<ILogger<ProcessApprovedSubmissionsFunction>> _loggerMock;
    private Mock<IServiceBusProvider> _serviceBusProviderMock;
    private ProcessApprovedSubmissionsFunction _function;
    private TimerInfo _timerInfo;

    [TestInitialize]
    public void Setup()
    {
        _timerInfo = new TimerInfo();
        _loggerMock = new Mock<ILogger<ProcessApprovedSubmissionsFunction>>();
        _serviceBusProviderMock = new Mock<IServiceBusProvider>();

        _function = new ProcessApprovedSubmissionsFunction(_loggerMock.Object, _serviceBusProviderMock.Object);
    }

    [TestMethod]
    public async Task RunAsync_ShouldProcessMessages_WhenInvoked()
    {
        // Arrange
        _serviceBusProviderMock
            .Setup(s => s.ReceiveAndProcessMessagesFromQueueAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _function.RunAsync(_timerInfo);

        // Assert
        // Verify that the method to receive and process messages was called
        _serviceBusProviderMock.Verify(s => s.ReceiveAndProcessMessagesFromQueueAsync(), Times.Once);

        // Verify log entries
        _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Exactly(2));
    }

    [TestMethod]
    public async Task RunAsync_ShouldLogError_WhenExceptionOccurs()
    {
        // Arrange
        _serviceBusProviderMock
            .Setup(s => s.ReceiveAndProcessMessagesFromQueueAsync())
            .ThrowsAsync(new Exception("Test Exception"));

        // Act
        await _function.RunAsync(_timerInfo);

        // Assert
        // Verify that the error is logged
        _loggerMock.Verify(log => log.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }
}