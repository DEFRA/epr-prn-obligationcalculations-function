using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;
using Microsoft.Identity.Web;

namespace EPR.PRN.ObligationCalculation.Function.Handlers;

[ExcludeFromCodeCoverage]
public class BaseServiceAuthorisationHandler : DelegatingHandler
{
	private readonly TokenRequestContext _tokenRequestContext;
	private readonly DefaultAzureCredential? _credentials;

	public BaseServiceAuthorisationHandler(string clientId)
	{
		if (string.IsNullOrEmpty(clientId))
		{
			return;
		}

		// _tokenRequestContext = new TokenRequestContext([clientId]);
		_tokenRequestContext = new TokenRequestContext(new[] { $"{clientId}/.default" });
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