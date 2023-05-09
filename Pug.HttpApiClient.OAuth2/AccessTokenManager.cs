using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Pug.HttpApiClient.Json;

namespace Pug.HttpApiClient.OAuth2
{
	public abstract class AccessTokenManager<TToken> : IAccessTokenManager<TToken>
		where TToken : AccessToken
	{
		private readonly SemaphoreSlim _accessTokenRequestSync = new ( 1, 1 );

		private TToken _accessToken;
		private DateTime _clientAccessTokenExpiryTimestamp = DateTime.MaxValue;

		protected AccessTokenManager( Uri oAuth2Endpoint, IHttpClientFactory httpClientFactory, TToken accessToken = null )
		{
			Oauth2Endpoint = oAuth2Endpoint;
			HttpClientFactory = httpClientFactory ?? throw new ArgumentNullException( nameof(httpClientFactory) );
			_accessToken = accessToken;
		}

		private bool NewAccessTokenRequired()
		{
			return _accessToken is null || _clientAccessTokenExpiryTimestamp <= DateTime.Now;
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
			{
				TToken token = _accessToken;
				
				_accessTokenRequestSync.Release();
				
				return token;
			}

			try
			{
				_accessToken = GetNewAccessToken();

				_clientAccessTokenExpiryTimestamp = DateTime.Now.AddSeconds( _accessToken.ValidityPeriod - 5 );
			}
			finally
			{
				_accessTokenRequestSync.Release();
			}

			return _accessToken;
		}

		public async Task<TToken> GetAccessTokenAsync()
		{
			await _accessTokenRequestSync.WaitAsync();

			if( !NewAccessTokenRequired() )
			{
				TToken token = _accessToken;
				
				_accessTokenRequestSync.Release();
				
				return token;
			}

			try
			{
				_accessToken = await GetNewAccessTokenAsync();

				_clientAccessTokenExpiryTimestamp = DateTime.Now.AddSeconds( _accessToken.ValidityPeriod - 5 );
			}
			finally
			{
				_accessTokenRequestSync.Release();
			}

			return _accessToken;
		}
	}
}