using System;
using System.Net.Http;

namespace Pug.HttpApiClient.OAuth2
{
	public abstract class ApiClientFactory<TClient>
		: IApiClientFactory<TClient>
	{
		public string ClientId { get; }
		public string OAuth2Scopes { get; }
		public Uri Url { get; }
		public Uri OAuth2ProviderUrl { get; }
		
		protected string ClientSecret { get; }
		protected IHttpClientFactory HttpClientFactory { get; }
		protected IHttpRequestMessageDecorator OAuthClientCredentialsDecorator { get; }

		protected ApiClientFactory( Uri url, Uri oAuth2ProviderUrl, string clientId, string clientSecret, string oAuth2Scopes,
									IHttpClientFactory httpClientFactory )
		{
			ClientId = clientId;
			ClientSecret = clientSecret;
			OAuth2Scopes = oAuth2Scopes;
			Url = url ?? throw new ArgumentNullException( nameof(url) );
			OAuth2ProviderUrl = oAuth2ProviderUrl ?? throw new ArgumentNullException( nameof(oAuth2ProviderUrl) );
			HttpClientFactory = httpClientFactory ?? throw new ArgumentNullException( nameof(httpClientFactory) );

			OAuthClientCredentialsDecorator = new ClientCredentialsDecorator(
				oAuth2ProviderUrl.ToString(),
				clientId, clientSecret, oAuth2Scopes,
				httpClientFactory );
		}

		protected abstract TClient GetClient( IHttpRequestMessageDecorator authenticationDecorator );

		public virtual TClient GetClient()
		{
			return GetClient( OAuthClientCredentialsDecorator );
		}

		public virtual TClient GetClient( ITokenExchangeSubjectTokenSource tokenExchangeSubjectTokenSource )
		{
			return GetClient(
					new TokenExchangeCredentialsDecorator( OAuth2ProviderUrl, ClientId, ClientSecret,
															OAuth2Scopes, tokenExchangeSubjectTokenSource,
															HttpClientFactory )
				);
		}

		public virtual TClient GetClient( RefreshableAccessToken refreshableAccessToken )
		{
			return GetClient(
					new AccessTokenMessageDecorator(
							new RefreshTokenManager( OAuth2ProviderUrl, ClientId, ClientSecret,
									OAuth2Scopes, refreshableAccessToken, HttpClientFactory)
						)
				);
		}

		[Obsolete("Use authorization code flow instead where possible")]
		public TClient GetClient( string username, string password )
		{
			return GetClient(
					new AccessTokenMessageDecorator(
							new PasswordAccessTokenManager( OAuth2ProviderUrl, ClientId, ClientSecret,
													username, password, OAuth2Scopes, HttpClientFactory)
						)
				);
		}
	}
}