using System;
using System.Net.Http;

namespace Pug.HttpApiClient.OAuth2
{
	public class ClientCredentialsDecorator : AccessTokenMessageDecorator
	{
		public ClientCredentialsDecorator( ClientAccessTokenManager accessTokenManager ) 
			: base(accessTokenManager)
		{
		}

		public ClientCredentialsDecorator( Uri oAuth2Endpoint, string clientId, string clientSecret, string scopes,
											IHttpClientFactory httpClientFactory )
			: this(
					new ClientAccessTokenManager( oAuth2Endpoint, clientId, clientSecret, scopes, httpClientFactory )
				)
		{
		}

		public ClientCredentialsDecorator( string oAuth2Endpoint, string clientId, string clientSecret, string scopes,
											IHttpClientFactory httpClientFactory )
			: this( new Uri( oAuth2Endpoint ), clientId, clientSecret, scopes, httpClientFactory )
		{
		}
	}
}