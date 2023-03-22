using System;
using System.Net.Http;

namespace Pug.HttpApiClient.OAuth2
{
	public class TokenExchangeCredentialsDecorator : AccessTokenMessageDecorator
	{
		public TokenExchangeCredentialsDecorator( TokenExchangeAccessTokenManager tokenManager ) 
			: base(tokenManager)
		{
		}

		public TokenExchangeCredentialsDecorator( Uri oAuth2Endpoint, string clientId, string clientSecret, string scopes,
												ITokenExchangeSubjectTokenSource tokenSource, IHttpClientFactory httpClientFactory )
			: this( new TokenExchangeAccessTokenManager( oAuth2Endpoint, clientId, clientSecret, scopes, tokenSource, httpClientFactory ) )
		{
			
		}
		
		public TokenExchangeCredentialsDecorator( string oAuth2Endpoint, string clientId, string clientSecret, string scopes,
												ITokenExchangeSubjectTokenSource tokenSource, IHttpClientFactory httpClientFactory )
			: this(new TokenExchangeAccessTokenManager( oAuth2Endpoint, clientId, clientSecret, scopes, tokenSource, httpClientFactory ))
		{
			
		}
		
		public TokenExchangeCredentialsDecorator( string oAuth2Endpoint, string clientId, string clientSecret, string scopes,
												string subjectToken, IHttpClientFactory httpClientFactory )
			: this(new TokenExchangeAccessTokenManager( oAuth2Endpoint, clientId, clientSecret, scopes, 
														new StaticTokenExchangeSubjectTokenSource( subjectToken ), httpClientFactory ))
		{
			
		}
	}
}