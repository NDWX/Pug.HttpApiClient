using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Pug.HttpApiClient.OAuth2
{
	public class RefreshTokenManager : AccessTokenManager<AccessToken>, IRefreshTokenManager
	{
		private readonly string _clientId;
		private readonly string _clientSecret;
		private readonly string _scopes;
		protected RefreshableAccessToken AccessToken { get; set; }
		protected IHttpRequestMessageDecorator ClientCredentialsDecorator { get; }

		public const string GrantType = "refresh_token";
		
		protected readonly MediaTypeWithQualityHeaderValue _jsonMediaType = new ( "*/*" );

		public string Scopes => _scopes;

		protected string ClientSecret => _clientSecret;

		public string ClientId => _clientId;
		
		protected RefreshTokenManager( Uri oAuth2Endpoint, string clientId, string clientSecret, string oAuth2Scopes,
										IHttpClientFactory httpClientFactory, Func<RefreshTokenManager, RefreshableAccessToken> refreshTokenSource ) 
			: base( oAuth2Endpoint, httpClientFactory )
		{
			if( string.IsNullOrWhiteSpace( clientId ) ) throw new ArgumentException( "Value cannot be null or whitespace.", nameof(clientId) );

			if( string.IsNullOrWhiteSpace( clientSecret ) )
				throw new ArgumentException( "Value cannot be null or whitespace.", nameof(clientSecret) );
			
			if( string.IsNullOrWhiteSpace( oAuth2Scopes ) ) throw new ArgumentException( "Value cannot be null or whitespace.", nameof(oAuth2Scopes) );
			
			_clientId = clientId;
			_clientSecret = clientSecret;
			_scopes = oAuth2Scopes;

			ClientCredentialsDecorator =
				new BasicAuthenticationMessageDecorator( clientId, clientSecret );

			AccessToken = refreshTokenSource(this) ?? throw new AccessTokenException();
		}

		public RefreshTokenManager( Uri oAuth2Endpoint, string clientId, string clientSecret, string oAuth2Scopes,
									RefreshableAccessToken refreshableAccessToken,
									IHttpClientFactory httpClientFactory ) 
			: this( oAuth2Endpoint, clientId, clientSecret, oAuth2Scopes, httpClientFactory, m => refreshableAccessToken )
		{
		}

		/// <remarks>
		/// Serves two roles: the constructor-time INITIAL grant for PasswordAccessTokenManager and
		/// AuthorizationCodeTokenManager - which is why it takes <paramref name="useClientCredentials"/>, as
		/// those grants authenticate with client_secret_basic - and the refresh grant. Returns
		/// <see cref="RefreshableAccessToken"/> so both roles can see the provider's refresh token.
		/// </remarks>
		protected internal virtual RefreshableAccessToken GetAccessToken( FormUrlEncodedContent formUrlEncodedContent, bool useClientCredentials = false )
		{
			OpenIdConfiguration openIdConfiguration = GetOpenIdConfiguration();

			HttpResponseMessage responseMessage;

			try
			{
				IHttpApiClient httpApiClient =
					new TokenEndpointHttpApiClient(
							openIdConfiguration.TokenEndpoint, HttpClientFactory,
							useClientCredentials? new[] { ClientCredentialsDecorator } : null
						);
				
				responseMessage =
					httpApiClient.PostAsync( string.Empty,
											formUrlEncodedContent,
											_jsonMediaType )
								.ConfigureAwait( false )
								.GetAwaiter()
								.GetResult();
			}
			catch( TaskCanceledException )
			{
				throw;
			}
			catch( Exception e )
			{
				if( e is AggregateException && e.InnerException is not null )
					throw e.InnerException;

				throw;
			}

			return HandleTokenResponse<RefreshableAccessToken>(
					responseMessage,
					responseMessage.Content.ReadAsStringAsync().ConfigureAwait( false ).GetAwaiter().GetResult()
				);
		}

		/// <remarks>
		/// Serves the REFRESH grant only, which is the one way it differs from the synchronous
		/// <see cref="GetAccessToken"/>: there is no <c>useClientCredentials</c> parameter and no Basic auth
		/// header, because the refresh grant built by <see cref="GetNewAccessTokenAsync"/> carries
		/// client_id/client_secret as form fields (client_secret_post). The sync overload needs that
		/// parameter because it ALSO serves the constructor-time initial grant for PasswordAccessTokenManager
		/// and AuthorizationCodeTokenManager, which authenticate with client_secret_basic. There is no async
		/// initial-grant path because those grants are issued from constructors, which cannot await.
		///
		/// Both overloads return <see cref="RefreshableAccessToken"/>, so a refresh token the provider
		/// rotated is visible to the caller and can be adopted by
		/// <see cref="RetainRotatedRefreshToken"/>. Deserializing a plain <see cref="AccessToken"/> here
		/// would make rotation impossible on the async path.
		/// </remarks>
		protected internal virtual async Task<RefreshableAccessToken> GetAccessTokenAsync( FormUrlEncodedContent formUrlEncodedContent)
		{
			OpenIdConfiguration openIdConfiguration = await GetOpenIdConfigurationAsync();
			HttpResponseMessage responseMessage;
			
			try
			{
				IHttpApiClient httpApiClient = new TokenEndpointHttpApiClient( openIdConfiguration.TokenEndpoint, HttpClientFactory );

				responseMessage =
					await httpApiClient.PostAsync( string.Empty, formUrlEncodedContent, _jsonMediaType, null, null );
			}
			catch( TaskCanceledException )
			{
				throw;
			}
			catch( Exception e )
			{
				if( e is AggregateException && e.InnerException is not null )
					throw e.InnerException;

				throw;
			}

			return HandleTokenResponse<RefreshableAccessToken>(
					responseMessage,
					await responseMessage.Content.ReadAsStringAsync()
				);
		}

		/// <summary>
		/// Adopt a refresh token the provider rotated, so the NEXT refresh presents the current token rather
		/// than replaying the one this manager was seeded with.
		/// </summary>
		/// <remarks>
		/// RFC 6749 section 6 lets an authorization server issue a new refresh token with each refresh
		/// response, and a server configured for single-use refresh tokens (Keycloak's "Revoke Refresh Token")
		/// rejects any token presented twice. Without this the second refresh fails with
		/// <c>invalid_grant</c>. A response that carries no refresh_token means the server did not rotate,
		/// so the existing token is kept.
		///
		/// Callers reach this while holding the base class's token semaphore, so the write is serialized.
		/// </remarks>
		/// <param name="refreshed">Token response received from the provider.</param>
		/// <returns><paramref name="refreshed"/>, unchanged.</returns>
		private RefreshableAccessToken RetainRotatedRefreshToken( RefreshableAccessToken refreshed )
		{
			if( !string.IsNullOrWhiteSpace( refreshed?.RefreshToken ) )
				AccessToken = refreshed;

			return refreshed;
		}

		protected override AccessToken GetNewAccessToken()
		{
			FormUrlEncodedContent formUrlEncodedContent = new (
					new Dictionary<string, string>
					{
						["grant_type"] = GrantType,
						["scope"] = _scopes,
						["refresh_token"] = AccessToken.RefreshToken,
						["client_id"] = ClientId,
						["client_secret"] = ClientSecret
					}
				);

			return RetainRotatedRefreshToken( GetAccessToken( formUrlEncodedContent ) );
		}

		protected override async Task<AccessToken> GetNewAccessTokenAsync()
		{
			FormUrlEncodedContent formUrlEncodedContent = new (
					new Dictionary<string, string>
					{
						["grant_type"] = GrantType,
						["scope"] = _scopes,
						["refresh_token"] = AccessToken.RefreshToken,
						["client_id"] = ClientId,
						["client_secret"] = ClientSecret
					}
				);

			return RetainRotatedRefreshToken( await GetAccessTokenAsync( formUrlEncodedContent ) );
		}
	}
}