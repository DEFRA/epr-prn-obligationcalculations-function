using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace EPR.PRN.ObligationCalculation.Function.IntegrationTests
{
    [TestFixture]
    [Category("IntegrationTest")]
    public class HealthCheckFunctionTests
    {
        private static HttpClient _httpClient = null!;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:7071") };
        }

        [Test]
        public async Task WhenHealthCheckRequest_ShouldReturnSuccess()
        {
            var response = await _httpClient.GetAsync("/api/health");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
