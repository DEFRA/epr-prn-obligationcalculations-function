﻿#nullable disable
using EPR.PRN.ObligationCalculation.Application.Services;
using EPR.PRN.ObligationCalculation.Function.UnitTests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace EPR.PRN.ObligationCalculation.Function.UnitTests;

[TestClass]
public class ProcessApprovedSubmissionsFunctionTests
{
    private Mock<ILogger<ProcessApprovedSubmissionsFunction>> _loggerMock;
    private Mock<IPrnService> _prnServiceMock;
    private ProcessApprovedSubmissionsFunction _function;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<ProcessApprovedSubmissionsFunction>>();
        _prnServiceMock = new Mock<IPrnService>();

        _function = new ProcessApprovedSubmissionsFunction(_loggerMock.Object, _prnServiceMock.Object);
    }

    [TestMethod]
    public async Task RunAsync_ShouldProcessMessageSuccessfully()
    {
        // Arrange
        var serviceBusMessage = ServiceBusModelBuilder.CreateServiceBusReceivedMessage("test-message-body");

        // Act
        await _function.RunAsync(serviceBusMessage);

        // Assert
        _prnServiceMock.Verify(service => service.ProcessApprovedSubmission("test-message-body"), Times.Once);
        _loggerMock.Verify(logger => logger.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Exactly(2));
    }

    [TestMethod]
    public async Task RunAsync_ShouldLogError_WhenExceptionOccurs()
    {
        // Arrange
        var serviceBusMessage = ServiceBusModelBuilder.CreateServiceBusReceivedMessage("test-message-body");

        _prnServiceMock.Setup(service => service.ProcessApprovedSubmission(It.IsAny<string>()))
                       .ThrowsAsync(new Exception("Test exception"));

        // Act & Assert
        await Assert.ThrowsExceptionAsync<Exception>(() => _function.RunAsync(serviceBusMessage));

        _loggerMock.Verify(logger => logger.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }
}