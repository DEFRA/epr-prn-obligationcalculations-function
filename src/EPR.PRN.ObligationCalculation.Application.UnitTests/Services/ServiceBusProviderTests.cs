using AutoFixture;
using Azure.Messaging.ServiceBus;
using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.DTOs;
using EPR.PRN.ObligationCalculation.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace EPR.PRN.ObligationCalculation.Application.UnitTests.Services;

[TestClass]
public class ServiceBusProviderTests
{
    private Mock<ILogger<ServiceBusProvider>> _loggerMock = null!;
    private Mock<ServiceBusClient> _serviceBusClientMock = null!;
    private Mock<IOptions<ServiceBusConfig>> _configMock = null!;
    private Mock<ServiceBusSender> _serviceBusSenderMock = null!;
    private ServiceBusProvider _serviceBusProvider = null!;
    private Mock<ServiceBusReceiver> _serviceBusReceiverMock = null!;
    private Fixture fixture = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        fixture = new Fixture();
        _loggerMock = new Mock<ILogger<ServiceBusProvider>>();
        _serviceBusClientMock = new Mock<ServiceBusClient>();
        _configMock = new Mock<IOptions<ServiceBusConfig>>();
        _serviceBusSenderMock = new Mock<ServiceBusSender>();
        _serviceBusReceiverMock = new Mock<ServiceBusReceiver>();

        var config = new ServiceBusConfig
        {
            ObligationQueueName = "test-queue-1",
            ObligationLastSuccessfulRunQueueName = "test-queue-2"
        };

        _configMock.Setup(c => c.Value).Returns(config);

        // Set up ServiceBusClient to return the mock receiver
        _serviceBusClientMock
            .Setup(client => client.CreateReceiver(It.IsAny<string>()))
            .Returns(_serviceBusReceiverMock.Object);

        _serviceBusProvider = new ServiceBusProvider(
            _loggerMock.Object,
            _serviceBusClientMock.Object,
            _configMock.Object
        );
    }

    [TestMethod]
    public async Task SendApprovedSubmissionsToQueueAsync_Success()
    {
        // Arrange
        _serviceBusClientMock.Setup(client => client.CreateSender(It.IsAny<string>())).Returns(_serviceBusSenderMock.Object);

        var approvedSubmissions = fixture.CreateMany<ApprovedSubmissionEntity>(3).ToList();
        var expectedLogMessage = "Messages have been published to the obligation queue.";
		// Act
		await _serviceBusProvider.SendApprovedSubmissionsToQueueAsync(approvedSubmissions);

        // Assert
        _serviceBusSenderMock.Verify(sender => sender.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default));
        _serviceBusSenderMock.Verify(r => r.DisposeAsync(), Times.Once);
        _loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
			It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains(expectedLogMessage)),
			It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [TestMethod]
    public async Task SendApprovedSubmissionsToQueueAsync_LogInformation_WhenNoSubmissions()
    {
        // Arrange
        var messageBatch = ServiceBusModelFactory.ServiceBusMessageBatch(500, []);

        _serviceBusSenderMock.Setup(sender => sender.CreateMessageBatchAsync(default)).ReturnsAsync(messageBatch);
        _serviceBusClientMock.Setup(client => client.CreateSender(It.IsAny<string>())).Returns(_serviceBusSenderMock.Object);

        var approvedSubmissions = new List<ApprovedSubmissionEntity>();

        // Act
        await _serviceBusProvider.SendApprovedSubmissionsToQueueAsync(approvedSubmissions);

        // Assert
        _loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}