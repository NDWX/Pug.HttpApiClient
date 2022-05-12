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
	public sealed class ImpersonationAccessTokenManager : AccessTokenManager<AccessToken>, IImpersonationAccessTokenManager
	{
		private readonly string _clientId;
		private readonly string _clientSecret;
		private readonly string _scopes;
		private readonly ITokenExchangeSubjectTokenSource _subjectTokenSource;

		private const string GrantType = "urn:ietf:params:oauth:grant-type:token-exchange",
							SubjectTokenType = "urn:ietf:params:oauth:token-type:access_token";
		
		private readonly MediaTypeWithQualityHeaderValue _jsonMediaType = new ( "*/*" );

		public ImpersonationAccessTokenManager( string oAuth2Endpoint, string clientId, string clientSecret, string scopes, 
												ITokenExchangeSubjectTokenSource subjectTokenSource,
												IHttpClientFactory httpClientFactory )
			: this(new Uri( oAuth2Endpoint ?? throw new ArgumentNullException( nameof(oAuth2Endpoint) ) ), 
					clientId, clientSecret, scopes, subjectTokenSource, httpClientFactory)
		{
			
		}
		
		public ImpersonationAccessTokenManager( Uri oAuth2Endpoint, string clientId, string clientSecret, string scopes, 
												ITokenExchangeSubjectTokenSource subjectTokenSource,
												IHttpClientFactory httpClientFactory ) 
			: base( oAuth2Endpoint, httpClientFactory )
		{
			if( string.IsNullOrWhiteSpace( clientId ) ) throw new ArgumentException( "Value cannot be null or whitespace.", nameof(clientId) );

			if( string.IsNullOrWhiteSpace( clientSecret ) )
				throw new ArgumentException( "Value cannot be null or whitespace.", nameof(clientSecret) );
			
			if( string.IsNullOrWhiteSpace( scopes ) ) throw new ArgumentException( "Value cannot be null or whitespace.", nameof(scopes) );
			
			_clientId = clientId;
			_clientSecret = clientSecret;
			_scopes = scopes;
			_subjectTokenSource = subjectTokenSource ?? throw new ArgumentNullException( nameof(subjectTokenSource) );
		}

		protected override AccessToken GetNewAccessToken()
		{
			OpenIdConfiguration openIdConfiguration = GetOpenIdConfiguration();
			
			HttpResponseMessage responseMessage;

			try
			{
				IHttpApiClient httpApiClient = new HttpApiClient( openIdConfiguration.TokenEndpoint, HttpClientFactory);
				
				responseMessage =
					httpApiClient.PostAsync( string.Empty, new FormUrlEncodedContent(
													new Dictionary<string, string>
													{
														["grant_type"] = GrantType,
														["client_id"] = _clientId,
														["client_secret"] = _clientSecret,
														["scope"] = _scopes,
														["subject_token"] = _subjectTokenSource.GetSubjectToken(),
														["subject_token_type"] = SubjectTokenType
													}
												), _jsonMediaType, null, null )
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

					return JsonConvert.DeserializeObject<AccessToken>( tokenJson );
				
				default:
					throw new HttpApiRequestException(
						$"Unexpected response status code received from OAuth2 provider: {( (int)responseMessage.StatusCode ).ToString()}", responseMessage );
			}
		}

		protected override async Task<AccessToken> GetNewAccessTokenAsync()
		{
			OpenIdConfiguration openIdConfiguration = await GetOpenIdConfigurationAsync();
			HttpResponseMessage responseMessage;

			try
			{
				IHttpApiClient httpApiClient = new HttpApiClient( openIdConfiguration.TokenEndpoint, HttpClientFactory);
				
				responseMessage =
					await httpApiClient.PostAsync( string.Empty, new FormUrlEncodedContent(
															new Dictionary<string, string>
															{
																["grant_type"] = GrantType,
																["client_id"] = _clientId,
																["client_secret"] = _clientSecret,
																["scope"] = _scopes,
																["subject_token"] = _subjectTokenSource.GetSubjectToken(),
																["subject_token_type"] = SubjectTokenType
															}
														), _jsonMediaType, null, null );

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
						$"Unexpected response status code received from OAuth2 provider: {( (int)responseMessage.StatusCode ).ToString()}", responseMessage );
			}
		}
	}
}