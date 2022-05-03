using System;
using System.Net.Http;
using Pug.HttpApiClient.OAuth2Decorators;

namespace Pug.HttpApiClient.OAuth2
{
	public abstract class ApiClientFactory<TClient> 
		: IApiClientFactory<TClient>
	{
		public Uri Url { get; }
		public Uri OAuth2ProviderUrl { get; }
		public string ClientId { get; }
		protected string ClientSecret { get; }
		protected IHttpClientFactory HttpClientFactory { get; }
		protected IHttpRequestMessageDecorator OAuthClientCredentialsDecorator { get; }

		protected ApiClientFactory( Uri url, Uri oAuth2ProviderUrl, string clientId, string clientSecret, string oAuth2Scopes, IHttpClientFactory httpClientFactory )
		{
			if( string.IsNullOrWhiteSpace( clientId ) ) throw new ArgumentException( "Value cannot be null or whitespace.", nameof(clientId) );
			if( string.IsNullOrEmpty( clientSecret ) ) throw new ArgumentException( "Value cannot be null or empty.", nameof(clientSecret) );

			Url = url ?? throw new ArgumentNullException( nameof(url) );
			OAuth2ProviderUrl = oAuth2ProviderUrl ?? throw new ArgumentNullException( nameof(oAuth2ProviderUrl) );
			ClientId = clientId;
			ClientSecret = clientSecret;
			HttpClientFactory = httpClientFactory ?? throw new ArgumentNullException( nameof(httpClientFactory) );

			OAuthClientCredentialsDecorator = new ClientCredentialsDecorator( oAuth2ProviderUrl.ToString(), clientId, clientSecret,
																			oAuth2Scopes, httpClientFactory );
		}

		public abstract TClient GetClient();

		public abstract TClient GetClient( ITokenExchangeSubjectTokenSource tokenExchangeSubjectTokenSource );
	}
}