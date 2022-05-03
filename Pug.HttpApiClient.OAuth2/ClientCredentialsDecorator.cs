using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Pug.HttpApiClient.OAuth2Decorators
{
	public class ClientCredentialsDecorator : IHttpRequestMessageDecorator
	{
		private readonly IClientAccessTokenManager _accessTokenManager;

		public ClientCredentialsDecorator( IClientAccessTokenManager accessTokenManager )
		{
			_accessTokenManager = accessTokenManager ?? throw new ArgumentNullException( nameof(accessTokenManager) );
		}

		public ClientCredentialsDecorator( string oAuth2Endpoint, string clientId, string clientSecret, string scopes,
												IHttpClientFactory httpClientFactory ) 
			: this( new ClientAccessTokenManager( oAuth2Endpoint, clientId, clientSecret, scopes, httpClientFactory ) )
		{
		}

		public ClientCredentialsDecorator( Uri oAuth2Endpoint, string clientId, string clientSecret, string scopes,
											IHttpClientFactory httpClientFactory ) 
			: this( new ClientAccessTokenManager( oAuth2Endpoint, clientId, clientSecret, scopes, httpClientFactory ) )
		{
		}

		public void Decorate( MessageDecorationContext context )
		{
			ClientAccessToken clientAccessToken = _accessTokenManager.GetAccessToken();

			context.RequestHeaders.Authorization = new AuthenticationHeaderValue( "Bearer", clientAccessToken.Token );
		}

		public async Task DecorateAsync( MessageDecorationContext context )
		{
			ClientAccessToken clientAccessToken = await _accessTokenManager.GetAccessTokenAsync().ConfigureAwait( false );

			context.RequestHeaders.Authorization = new AuthenticationHeaderValue( "Bearer", clientAccessToken.Token );
		}
	}
}