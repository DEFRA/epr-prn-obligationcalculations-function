using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;
using EPR.PRN.ObligationCalculation.Application.Configs;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;

namespace EPR.PRN.ObligationCalculation.Function.Handlers;

[ExcludeFromCodeCoverage]
public class PrnServiceAuthorisationHandler : DelegatingHandler
{
	private readonly TokenRequestContext _tokenRequestContext;
	private readonly DefaultAzureCredential? _credentials;

	public PrnServiceAuthorisationHandler(IOptions<PrnServiceApiConfig> config)
	{
		if (string.IsNullOrEmpty(config.Value.ClientId))
		{
			return;
		}

		_tokenRequestContext = new TokenRequestContext([config.Value.ClientId]);
		_credentials = new DefaultAzureCredential();
	}

	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (_credentials != null)
		{
			var tokenResult = await _credentials.GetTokenAsync(_tokenRequestContext, cancellationToken);
			request.Headers.Authorization = new AuthenticationHeaderValue(Constants.Bearer, tokenResult.Token);
		}

		return await base.SendAsync(request, cancellationToken);
	}
}