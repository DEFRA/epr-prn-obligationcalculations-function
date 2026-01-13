using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Function.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using System.Net;

namespace EPR.PRN.ObligationCalculation.Function.UnitTests.Services;

[TestClass]
public class SubmissionsDataServiceTests
{
    private Mock<ILogger<EprCommonDataApiService>> _mockLogger = null!;
    private Mock<IOptions<SubmissionsServiceApiConfig>> _mockConfig = null!;
    private Mock<HttpMessageHandler> _httpMessageHandlerMock = null!;
    private HttpClient _httpClient = null!;
    private EprCommonDataApiService _underTest = null!;

    private readonly string _lastSuccessfulRunDate = "2023-09-01";

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<EprCommonDataApiService>>();
        _mockConfig = new Mock<IOptions<SubmissionsServiceApiConfig>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        
        var config = new SubmissionsServiceApiConfig
        {
            BaseUrl = "https://api.example.com/",
            SubmissionsEndPoint = "submissions"
        };
        _mockConfig.Setup(c => c.Value).Returns(config);
        
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri(_mockConfig.Object.Value.BaseUrl)
        };

        _underTest = new EprCommonDataApiService(_mockLogger.Object, _httpClient, _mockConfig.Object);
    }

    [TestMethod]
    public async Task GetSubmissions_ShouldReturnValidData_WhenApiResponseIsSuccessful()
    {
        // Arrange
        var expectedLogMessage = $"Get Approved Submissions Data from {_lastSuccessfulRunDate}";
        var submitterId = Guid.NewGuid();
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(new[]
                            {
	                            new { SubmitterId = submitterId }
                            }))
			});

        // Act
        var result = await _underTest.GetApprovedSubmissionsData(_lastSuccessfulRunDate);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(1);
        result[0].SubmitterId.Should().Be(submitterId);

        _mockLogger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedLogMessage)),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
    }

    [TestMethod]
    public async Task GetSubmissions_ShouldReturnEmptyList_WhenApiResponseIsEmptyString()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NoContent,
                Content = new StringContent(string.Empty)
            });

        // Act
        var result = await _underTest.GetApprovedSubmissionsData(_lastSuccessfulRunDate);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(0);
    }

    [TestMethod]
    public async Task GetSubmissions_ShouldThrowException_WhenHttpClientThrowsException()
    {
		_httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new Exception("Test Exception"));

        // Act & Assert
        _ = await Assert.ThrowsExceptionAsync<Exception>(() =>
            _underTest.GetApprovedSubmissionsData(_lastSuccessfulRunDate));

        // Assert handled by ExpectedException
        _mockLogger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}