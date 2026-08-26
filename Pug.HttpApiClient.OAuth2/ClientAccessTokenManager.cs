using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Pug.HttpApiClient.OAuth2
{
	public sealed class ClientAccessTokenManager : AccessTokenManager<AccessToken>, IClientAccessTokenManager
	{
		private readonly BasicAuthenticationMessageDecorator _clientCredentialsMessageDecorator;
		private readonly MediaTypeWithQualityHeaderValue _jsonMediaType = new ( "*/*" );
		// Held as the raw field set and turned into a fresh FormUrlEncodedContent per request.
		//
		// This was changed from a cached FormUrlEncodedContent on the belief that HttpClient disposes request
		// content after sending, making the second token request throw ObjectDisposedException. MUTATION
		// TESTING DISPROVED THAT: re-introducing the cached instance breaks nothing, in the stub-backed tests
		// or against real Keycloak. FormUrlEncodedContent derives from ByteArrayContent, whose byte[] survives
		// disposal, so it is safely re-sendable - the "never reuse HttpContent" rule applies to stream-backed
		// content, not this.
		//
		// Kept because it costs nothing (token requests are infrequent, the payload is tiny) and it matches
		// RefreshTokenManager and TokenExchangeAccessTokenManager, which already build their content per call.
		// Reverting to a cached instance would also be correct.
		private readonly IDictionary<string, string> _clientTokenRequestFields;
		
		public ClientAccessTokenManager( Uri oAuth2Endpoint, string clientId, string clientSecret, string scopes,
										IHttpClientFactory httpClientFactory ) 
			: base(oAuth2Endpoint, httpClientFactory)
		{
			if( string.IsNullOrWhiteSpace( clientId ) ) throw new ArgumentException( "Value cannot be null or whitespace.", nameof(clientId) );

			if( string.IsNullOrWhiteSpace( clientSecret ) )
				throw new ArgumentException( "Value cannot be null or whitespace.", nameof(clientSecret) );

			if( scopes is null ) throw new ArgumentNullException( nameof(scopes) );
			ClientId = clientId;

			_clientCredentialsMessageDecorator = new BasicAuthenticationMessageDecorator( clientId, clientSecret );
			_clientTokenRequestFields = new Dictionary<string, string>
				{
					["grant_type"] = "client_credentials",
					["scope"] = scopes
				};
		}
		
		public string ClientId { get; }
		
		protected override AccessToken GetNewAccessToken()
		{
			HttpResponseMessage responseMessage;

			OpenIdConfiguration openIdConfiguration = GetOpenIdConfiguration();
			
			try
			{
				IHttpApiClient httpApiClient = new TokenEndpointHttpApiClient(
						openIdConfiguration.TokenEndpoint,
						HttpClientFactory,
						new IHttpRequestMessageDecorator[] { _clientCredentialsMessageDecorator }
					);

				responseMessage = httpApiClient.PostAsync(
													string.Empty , new FormUrlEncodedContent( _clientTokenRequestFields ), _jsonMediaType,
													null, null )

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

			return HandleTokenResponse<AccessToken>(
					responseMessage,
					responseMessage.Content.ReadAsStringAsync().ConfigureAwait( false ).GetAwaiter().GetResult()
				);
		}

		protected override async Task<AccessToken> GetNewAccessTokenAsync()
		{
			HttpResponseMessage responseMessage;

			OpenIdConfiguration openIdConfiguration = await GetOpenIdConfigurationAsync();
			
			try
			{
				IHttpApiClient httpApiClient = new TokenEndpointHttpApiClient(
						openIdConfiguration.TokenEndpoint,
						HttpClientFactory,
						new IHttpRequestMessageDecorator[] { _clientCredentialsMessageDecorator }
					);
				
				responseMessage =
					await httpApiClient.PostAsync( string.Empty, new FormUrlEncodedContent( _clientTokenRequestFields ), _jsonMediaType, null, null );

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

			return HandleTokenResponse<AccessToken>(
					responseMessage,
					await responseMessage.Content.ReadAsStringAsync()
				);
		}
	}
}