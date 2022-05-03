using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Pug.HttpApiClient.OAuth2Decorators
{
	public class TokenExchangeCredentialsDecorator : IHttpRequestMessageDecorator
	{
		private readonly IImpersonationAccessTokenManager _tokenManager;

		public TokenExchangeCredentialsDecorator( IImpersonationAccessTokenManager tokenManager )
		{
			_tokenManager = tokenManager;
		}

		public TokenExchangeCredentialsDecorator( Uri oAuth2Endpoint, string clientId, string clientSecret, string scopes,
												ITokenExchangeSubjectTokenSource tokenSource, IHttpClientFactory httpClientFactory )
			: this( new ImpersonationAccessTokenManager( oAuth2Endpoint, clientId, clientSecret, scopes, tokenSource, httpClientFactory ) )
		{
			
		}
		
		public TokenExchangeCredentialsDecorator( string oAuth2Endpoint, string clientId, string clientSecret, string scopes,
												ITokenExchangeSubjectTokenSource tokenSource, IHttpClientFactory httpClientFactory )
			: this(new ImpersonationAccessTokenManager( oAuth2Endpoint, clientId, clientSecret, scopes, tokenSource, httpClientFactory ))
		{
			
		}
		
		public TokenExchangeCredentialsDecorator( string oAuth2Endpoint, string clientId, string clientSecret, string scopes,
												string subjectToken, IHttpClientFactory httpClientFactory )
			: this(new ImpersonationAccessTokenManager( oAuth2Endpoint, clientId, clientSecret, scopes, 
														new StaticTokenExchangeSubjectTokenSource( subjectToken ), httpClientFactory ))
		{
			
		}

		public void Decorate( MessageDecorationContext context )
		{
			AccessToken clientAccessToken = _tokenManager.GetAccessToken();

			context.RequestHeaders.Authorization = new AuthenticationHeaderValue( "Bearer", clientAccessToken.Token );
		}

		public async Task DecorateAsync( MessageDecorationContext context )
		{
			AccessToken clientAccessToken = await _tokenManager.GetAccessTokenAsync().ConfigureAwait( false );

			context.RequestHeaders.Authorization = new AuthenticationHeaderValue( "Bearer", clientAccessToken.Token );
		}
	}
}