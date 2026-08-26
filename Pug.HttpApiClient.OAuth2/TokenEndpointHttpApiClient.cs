using System;
using System.Net.Http;

namespace Pug.HttpApiClient.OAuth2
{
	/// <summary>
	/// <see cref="HttpApiClient"/> for OAuth2 token endpoints, where 400 and 401 responses are part of the
	/// protocol rather than transport failures: RFC 6749 section 5.2 carries the failure reason in the
	/// response body, and providers differ on which of the two they use for bad client credentials.
	/// Suppressing the default status mapping keeps the response intact so the caller can read it.
	/// </summary>
	internal sealed class TokenEndpointHttpApiClient : HttpApiClient
	{
		public TokenEndpointHttpApiClient( string baseUrl, IHttpClientFactory httpClientFactory,
											IHttpRequestMessageDecorator[] messageDecorators = null )
			: base( new Uri( baseUrl ), httpClientFactory, null, messageDecorators )
		{
		}

		protected override void EnsureSuccessResponse( HttpRequestMessage requestMessage, HttpResponseMessage responseMessage )
		{
		}
	}
}
