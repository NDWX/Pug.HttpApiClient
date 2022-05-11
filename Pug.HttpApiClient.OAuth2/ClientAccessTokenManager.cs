using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Security.Authentication;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Pug.HttpApiClient.OAuth2Decorators
{
	internal sealed class ClientAccessTokenManager : AccessTokenManager<ClientAccessToken>, IClientAccessTokenManager
	{
		private readonly BasicAuthenticationMessageDecorator _clientCredentialsMessageDecorator;
		private readonly MediaTypeWithQualityHeaderValue _jsonMediaType = new ( "*/*" );
		private readonly FormUrlEncodedContent _clientTokenRequestContent;

		public ClientAccessTokenManager( string oAuth2Endpoint, string clientId, string clientSecret, string scopes,
										IHttpClientFactory httpClientFactory ) 
			: this(new Uri( oAuth2Endpoint ?? throw new ArgumentNullException( nameof(oAuth2Endpoint) ) ), 
					clientId, clientSecret, scopes, httpClientFactory)
		{
		}
		
		public ClientAccessTokenManager( Uri oAuth2Endpoint, string clientId, string clientSecret, string scopes,
										IHttpClientFactory httpClientFactory ) 
			: base(oAuth2Endpoint, httpClientFactory)
		{
			if( string.IsNullOrWhiteSpace( clientId ) ) throw new ArgumentException( "Value cannot be null or whitespace.", nameof(clientId) );

			if( string.IsNullOrWhiteSpace( clientSecret ) )
				throw new ArgumentException( "Value cannot be null or whitespace.", nameof(clientSecret) );

			if( scopes is null ) throw new ArgumentNullException( nameof(scopes) );

			_clientCredentialsMessageDecorator = new BasicAuthenticationMessageDecorator( clientId, clientSecret );
			_clientTokenRequestContent = new FormUrlEncodedContent(
					new Dictionary<string, string>
					{
						["grant_type"] = "client_credentials",
						["scope"] = scopes
					}
				);
			
		}
		
		protected override ClientAccessToken GetNewAccessToken()
		{
			HttpResponseMessage responseMessage;

			OpenIdConfiguration openIdConfiguration = GetOpenIdConfiguration();
			
			try
			{
				IHttpApiClient httpApiClient = new HttpApiClient(
						openIdConfiguration.TokenEnndpoint,
						HttpClientFactory,
						messageDecorators: new[] { _clientCredentialsMessageDecorator }
					);

				responseMessage = httpApiClient.PostAsync(
													string.Empty , _clientTokenRequestContent, _jsonMediaType,
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

			// ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
			switch( responseMessage.StatusCode )
			{
				case HttpStatusCode.BadRequest:

					TokenRequestError tokenRequestError = JsonConvert.DeserializeObject<TokenRequestError>(
							responseMessage.Content.ReadAsStringAsync().ConfigureAwait( false ).GetAwaiter().GetResult()
						);

					throw new AuthenticationException( tokenRequestError.Message );

				case HttpStatusCode.InternalServerError:

					throw new HttpApiRequestException(responseMessage);

				case HttpStatusCode.OK:
					string tokenJson = responseMessage.Content.ReadAsStringAsync().ConfigureAwait( false ).GetAwaiter().GetResult();

					return JsonConvert.DeserializeObject<ClientAccessToken>( tokenJson );
				
				default:
					throw new HttpApiRequestException(
						$"Unexpected response status code received from OAuth2 provider: {( (int)responseMessage.StatusCode ).ToString()}", responseMessage );
			}
		}

		protected override async Task<ClientAccessToken> GetNewAccessTokenAsync()
		{
			HttpResponseMessage responseMessage;

			OpenIdConfiguration openIdConfiguration = await GetOpenIdConfigurationAsync();
			
			try
			{
				IHttpApiClient httpApiClient = new HttpApiClient(
						openIdConfiguration.TokenEnndpoint,
						HttpClientFactory,
						messageDecorators: new[] { _clientCredentialsMessageDecorator }
					);
				
				responseMessage =
					await httpApiClient.PostAsync( string.Empty, _clientTokenRequestContent, _jsonMediaType, null, null );

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

					return JsonConvert.DeserializeObject<ClientAccessToken>( tokenJson );
				
				default:
					throw new HttpApiRequestException(
						$"Unexpected response status code received from OAuth2 provider: {( (int)responseMessage.StatusCode ).ToString()}", responseMessage );
			}
		}
	}
}