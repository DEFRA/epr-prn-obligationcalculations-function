using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Function.Services;
using Newtonsoft.Json;
using Moq;
using Moq.Protected;
using EPR.PRN.ObligationCalculation.Application.DTOs;

namespace EPR.PRN.ObligationCalculation.Function.UnitTests.Services;

[TestClass]
public class EprPrnCommonBackendServiceTests
{
    private Mock<ILogger<EprPrnCommonBackendService>> _mockLogger = null!;
    private Mock<HttpMessageHandler> _httpMessageHandlerMock = null!;
    private Mock<IOptions<PrnServiceApiConfig>> _mockConfig = null!;
    private HttpClient _httpClient = null!;
    private EprPrnCommonBackendService _underTest = null!;
    private PrnServiceApiConfig _config = null!;
    private string _submissionJson = string.Empty;

	[TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<EprPrnCommonBackendService>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _mockConfig = new Mock<IOptions<PrnServiceApiConfig>>();

        // Setup config
        _config = new PrnServiceApiConfig
        {
            BaseUrl = "http://test-url.com/",
            PrnCalculateEndPoint = "api/calculate/{0}"
        };

        _mockConfig.Setup(c => c.Value).Returns(_config);

        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri(_mockConfig.Object.Value.BaseUrl)
        };

        _underTest = new EprPrnCommonBackendService(_mockLogger.Object, _httpClient, _mockConfig.Object);

		_submissionJson = JsonConvert.SerializeObject(new List<ApprovedSubmissionEntity>
		{
			new () { SubmitterId = Guid.NewGuid() }
		});
	}

    [TestMethod]
    public async Task ProcessApprovedSubmission_ShouldLogInformation_WhenSubmissionIsEmpty()
    {
        // Arrange
        string emptySubmission = string.Empty;

        // Act
        await _underTest.CalculateApprovedSubmission(emptySubmission);

        // Assert
        _mockLogger.Verify(l => l.Log(
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
        await _underTest.CalculateApprovedSubmission(_submissionJson);

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
            _underTest.CalculateApprovedSubmission(_submissionJson));
        
        // Assert handled by ExpectedException
        _mockLogger.Verify(l => l.Log(
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
            _underTest.CalculateApprovedSubmission(_submissionJson));

        // Assert handled by ExpectedException
        _mockLogger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}