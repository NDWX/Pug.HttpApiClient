using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Pug.HttpApiClient.Json;

namespace Pug.HttpApiClient.OAuth2Decorators
{
	public abstract class AccessTokenManager<TToken> where TToken : AccessToken
	{
		private readonly SemaphoreSlim _accessTokenRequestSync = new ( 1, 1 );

		private TToken _clientAccessToken;
		private DateTime _clientAccessTokenExpiryTimestamp = DateTime.MaxValue;

		protected AccessTokenManager( Uri oAuth2Endpoint, IHttpClientFactory httpClientFactory )
		{
			Oauth2Endpoint = oAuth2Endpoint;
			HttpClientFactory = httpClientFactory ?? throw new ArgumentNullException( nameof(httpClientFactory) );
		}

		private bool NewAccessTokenRequired()
		{
			return _clientAccessToken is null || _clientAccessTokenExpiryTimestamp <= DateTime.Now;
		}

		protected Uri Oauth2Endpoint { get; }

		protected IHttpClientFactory HttpClientFactory { get; }

		protected OpenIdConfiguration GetOpenIdConfiguration()
		{
			IHttpApiClient apiClient = new HttpApiClient( Oauth2Endpoint, HttpClientFactory );

			return apiClient.GetAsync<OpenIdConfiguration>( @"/.well-known/openid-configuration" )
							.ConfigureAwait( false )
							.GetAwaiter()
							.GetResult();
		}

		protected Task<OpenIdConfiguration> GetOpenIdConfigurationAsync()
		{
			IHttpApiClient apiClient = new HttpApiClient( Oauth2Endpoint, HttpClientFactory );

			return apiClient.GetAsync<OpenIdConfiguration>( @"/.well-known/openid-configuration" );
		}
		
		protected abstract TToken GetNewAccessToken();

		protected abstract Task<TToken> GetNewAccessTokenAsync();

		public TToken GetAccessToken()
		{
			_accessTokenRequestSync.Wait();

			if( !NewAccessTokenRequired() )
				return _clientAccessToken;

			try
			{
				_clientAccessToken = GetNewAccessToken();

				_clientAccessTokenExpiryTimestamp = DateTime.Now.AddSeconds( _clientAccessToken.ValidityPeriod - 5 );
			}
			finally
			{
				_accessTokenRequestSync.Release();
			}

			return _clientAccessToken;
		}

		public async Task<TToken> GetAccessTokenAsync()
		{
			await _accessTokenRequestSync.WaitAsync();

			if( !NewAccessTokenRequired() )
				return _clientAccessToken;

			try
			{
				_clientAccessToken = await GetNewAccessTokenAsync();

				_clientAccessTokenExpiryTimestamp = DateTime.Now.AddSeconds( _clientAccessToken.ValidityPeriod - 5 );
			}
			finally
			{
				_accessTokenRequestSync.Release();
			}

			return _clientAccessToken;
		}
	}
}