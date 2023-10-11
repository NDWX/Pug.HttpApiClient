using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Threading.Tasks;
using Newtonsoft.Json;

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

		protected internal virtual RefreshableAccessToken GetAccessToken( FormUrlEncodedContent formUrlEncodedContent, bool useClientCredentials = false )
		{
			OpenIdConfiguration openIdConfiguration = GetOpenIdConfiguration();

			HttpResponseMessage responseMessage;

			try
			{
				IHttpApiClient httpApiClient =
					new HttpApiClient(
							openIdConfiguration.TokenEndpoint, HttpClientFactory,
							messageDecorators: useClientCredentials? new[] { ClientCredentialsDecorator } : null
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

			// ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
			switch( responseMessage.StatusCode )
			{
				case HttpStatusCode.BadRequest:

					TokenRequestError tokenRequestError = JsonConvert.DeserializeObject<TokenRequestError>(
							responseMessage.Content.ReadAsStringAsync().ConfigureAwait( false ).GetAwaiter().GetResult()
						);

					throw new AuthenticationException( tokenRequestError.Message );

				case HttpStatusCode.InternalServerError:

					throw new HttpApiRequestException( responseMessage );

				case HttpStatusCode.OK:
					string tokenJson = responseMessage.Content.ReadAsStringAsync().ConfigureAwait( false ).GetAwaiter().GetResult();

					return JsonConvert.DeserializeObject<RefreshableAccessToken>( tokenJson );

				default:
					throw new HttpApiRequestException(
						$"Unexpected response status code received from OAuth2 provider: {( (int)responseMessage.StatusCode ).ToString()}",
						responseMessage );
			}
		}

		protected internal virtual async Task<AccessToken> GetAccessTokenAsync( FormUrlEncodedContent formUrlEncodedContent)
		{
			OpenIdConfiguration openIdConfiguration = await GetOpenIdConfigurationAsync();
			HttpResponseMessage responseMessage;
			try
			{
				IHttpApiClient httpApiClient = new HttpApiClient( openIdConfiguration.TokenEndpoint, HttpClientFactory );

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

			// ReSharper disable once 
			switch( responseMessage.StatusCode )
			{
				case HttpStatusCode.BadRequest:

					TokenRequestError tokenRequestError = JsonConvert.DeserializeObject<TokenRequestError>(
							await responseMessage.Content.ReadAsStringAsync()
						);

					throw new AuthenticationException( tokenRequestError.Message );

				case HttpStatusCode.InternalServerError:

					throw new HttpRequestException();

				case HttpStatusCode.OK:
					string tokenJson = await responseMessage.Content.ReadAsStringAsync();

					return JsonConvert.DeserializeObject<AccessToken>( tokenJson );

				default:
					throw new HttpApiRequestException(
						$"Unexpected response status code received from OAuth2 provider: {( (int)responseMessage.StatusCode ).ToString()}",
						responseMessage );
			}
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

			return GetAccessToken( formUrlEncodedContent );
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

			return await GetAccessTokenAsync( formUrlEncodedContent );
		}
	}
}