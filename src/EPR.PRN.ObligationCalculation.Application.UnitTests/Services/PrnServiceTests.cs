using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.Services;
using Newtonsoft.Json;
using Moq;
using Moq.Protected;
using EPR.PRN.ObligationCalculation.Application.DTOs;

namespace EPR.PRN.ObligationCalculation.Application.UnitTests.Services;

[TestClass]
public class PrnServiceTests
{
    private Mock<ILogger<PrnService>> _loggerMock = null!;
    private Mock<HttpMessageHandler> _httpMessageHandlerMock = null!;
    private Mock<IOptions<PrnServiceApiConfig>> _configMock = null!;
    private HttpClient _httpClient = null!;
    private PrnService _prnService = null!;
    private PrnServiceApiConfig _config = null!;
    private string _submissionJson = string.Empty;

	[TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<PrnService>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _configMock = new Mock<IOptions<PrnServiceApiConfig>>();

        // Setup config
        _config = new PrnServiceApiConfig
        {
            BaseUrl = "http://test-url.com/",
            PrnCalculateEndPoint = "api/calculate/{0}"
        };

        _configMock.Setup(c => c.Value).Returns(_config);

        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri(_configMock.Object.Value.BaseUrl)
        };

        _prnService = new PrnService(_loggerMock.Object, _httpClient, _configMock.Object);

		_submissionJson = JsonConvert.SerializeObject(new List<ApprovedSubmissionEntity>
		{
			new () { ParentId = Guid.NewGuid() }
		});
	}

    [TestMethod]
    public async Task ProcessApprovedSubmission_ShouldLogInformation_WhenSubmissionIsEmpty()
    {
        // Arrange
        string emptySubmission = string.Empty;

        // Act
        await _prnService.ProcessApprovedSubmission(emptySubmission);

        // Assert
        _loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Exactly(1));
    }

    [TestMethod]
    public async Task ProcessApprovedSubmission_ShouldSendPostRequest_WhenSubmissionIsNotEmpty()
    {
        // Arrange
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        // Act
        await _prnService.ProcessApprovedSubmission(_submissionJson);

        // Assert
        _httpMessageHandlerMock
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Post && r.RequestUri!.ToString().Contains(_config.BaseUrl)),
                ItExpr.IsAny<CancellationToken>());
    }

    [TestMethod]
    public async Task ProcessApprovedSubmission_ShouldThrowHttpRequestException_WhenUnsuccesfulResponse()
    {
        // Arrange
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Act & Assert
        _ = await Assert.ThrowsExceptionAsync<HttpRequestException>(() =>
            _prnService.ProcessApprovedSubmission(_submissionJson));
        
        // Assert handled by ExpectedException
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [TestMethod]
    public async Task ProcessApprovedSubmission_ShouldThrowException_WhenHttpClientThrowsException()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new Exception("Test Exception"));

        // Act & Assert
        _ = await Assert.ThrowsExceptionAsync<Exception>(() =>
            _prnService.ProcessApprovedSubmission(_submissionJson));

        // Assert handled by ExpectedException
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}