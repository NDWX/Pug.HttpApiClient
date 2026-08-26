using System;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Pug.HttpApiClient.Json;
#if NETCOREAPP2_1 || NETSTANDARD
using Newtonsoft.Json;
#else
using System.Text.Json;
#endif

namespace Pug.HttpApiClient.OAuth2
{
	public abstract class AccessTokenManager<TToken> : IAccessTokenProvider<TToken>
		where TToken : AccessToken
	{
		/// <summary>
		/// Seconds shaved off a token's stated validity period, so a token is treated as expired slightly
		/// before the provider would reject it and an in-flight request cannot race the expiry.
		/// </summary>
		private const int ExpirySafetyMarginSeconds = 5;

		private readonly SemaphoreSlim _accessTokenRequestSync = new ( 1, 1 );

		private TToken _accessToken;
		private DateTime _clientAccessTokenExpiryTimestamp;

		protected AccessTokenManager( Uri oAuth2Endpoint, IHttpClientFactory httpClientFactory, TToken accessToken = null )
		{
			Oauth2Endpoint = oAuth2Endpoint;
			HttpClientFactory = httpClientFactory ?? throw new ArgumentNullException( nameof(httpClientFactory) );
			_accessToken = accessToken;

			// A seeded token expires on its own ValidityPeriod like any other. Previously this was
			// initialised to DateTime.MaxValue, which cached a constructor-supplied token forever however
			// short its expires_in actually was. When no token is seeded the value is irrelevant, because
			// NewAccessTokenRequired() short-circuits on the null token.
			_clientAccessTokenExpiryTimestamp = accessToken is null
													? DateTime.MinValue
													: ExpiryTimestampFor( accessToken );
		}

		private static DateTime ExpiryTimestampFor( TToken accessToken ) =>
			DateTime.Now.AddSeconds( accessToken.ValidityPeriod - ExpirySafetyMarginSeconds );

		private bool NewAccessTokenRequired()
		{
			return _accessToken is null || _clientAccessTokenExpiryTimestamp <= DateTime.Now;
		}

		protected Uri Oauth2Endpoint { get; }

		protected IHttpClientFactory HttpClientFactory { get; }

		/// <remarks>
		/// DIVERGENCE - the discovery document is fetched fresh on EVERY call, and every concrete manager
		/// calls this once per token request. So each token acquisition costs two round trips instead of one,
		/// and a provider that rate-limits its discovery endpoint sees twice the traffic it needs to.
		/// OpenID discovery documents are explicitly cacheable and change rarely.
		///
		/// No rationale is recorded; the simplest reading is that caching was never added rather than
		/// deliberately avoided. Left as-is because adding a cache introduces a staleness policy (how long,
		/// and how to invalidate when a provider rotates endpoints) that is a design decision, not a fix.
		/// The stub-backed tests assert the two-hop order, so they will show the change if it is ever made.
		/// </remarks>
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
		
		/// <summary>
		/// Interpret a token endpoint response. A 200 yields the deserialized token; 400 and 401 are
		/// RFC 6749 error responses and surface as <see cref="AuthenticationException"/> carrying the
		/// provider's own <c>error</c>/<c>error_description</c>; anything else is unexpected.
		/// </summary>
		/// <param name="responseMessage">Response received from the token endpoint.</param>
		/// <param name="responseBody">Body of <paramref name="responseMessage"/>, already read.</param>
		/// <typeparam name="TResult">Token type to deserialize a successful response into.</typeparam>
		protected static TResult HandleTokenResponse<TResult>( HttpResponseMessage responseMessage, string responseBody )
		{
			switch( responseMessage.StatusCode )
			{
				case HttpStatusCode.OK:
#if NETCOREAPP2_1 || NETSTANDARD
					return JsonConvert.DeserializeObject<TResult>( responseBody );
#else
					return JsonSerializer.Deserialize<TResult>( responseBody );
#endif

				// Providers disagree on which of these signals bad client credentials: the specification
				// says 400, but a provider that authenticates the client via HTTP Basic MAY answer 401.
				case HttpStatusCode.BadRequest:
				case HttpStatusCode.Unauthorized:
					throw CreateTokenRequestException( responseMessage, responseBody );

				default:
					throw new HttpApiRequestException(
						$"Unexpected response status code received from OAuth2 provider: {( (int)responseMessage.StatusCode ).ToString()}",
						responseMessage );
			}
		}

		private static Exception CreateTokenRequestException( HttpResponseMessage responseMessage, string responseBody )
		{
			TokenRequestError tokenRequestError;

			try
			{
#if NETCOREAPP2_1 || NETSTANDARD
				tokenRequestError = JsonConvert.DeserializeObject<TokenRequestError>( responseBody );
#else
				tokenRequestError = JsonSerializer.Deserialize<TokenRequestError>( responseBody );
#endif
			}
			catch( Exception )
			{
				// not a well-formed OAuth2 error document - surface the raw response instead of a parse failure
				return new HttpApiRequestException( responseMessage );
			}

			if( string.IsNullOrWhiteSpace( tokenRequestError?.Message ) )
				return new HttpApiRequestException( responseMessage );

			return new AuthenticationException( tokenRequestError.FullMessage );
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

				_clientAccessTokenExpiryTimestamp = ExpiryTimestampFor( _accessToken );
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

				_clientAccessTokenExpiryTimestamp = ExpiryTimestampFor( _accessToken );
			}
			finally
			{
				_accessTokenRequestSync.Release();
			}

			return _accessToken;
		}
	}
}