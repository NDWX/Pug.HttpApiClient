using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Pug.HttpApiClient.OAuth2
{
	/// <remarks>
	/// DIVERGENCE - this manager authenticates the client with <c>client_secret_post</c> (client_id and
	/// client_secret as form fields in the request body), whereas ClientAccessTokenManager and
	/// RefreshTokenManager use <c>client_secret_basic</c> (an HTTP Basic header, via
	/// BasicAuthenticationMessageDecorator). No decorator is attached to the token request here.
	///
	/// Unlike the other divergences recorded in this assembly, this one is defensible: RFC 6749 section 2.3.1
	/// permits both methods, and RFC 8693 token-exchange examples conventionally show the body form. It is
	/// documented only because the three managers in this assembly now authenticate the client three
	/// different ways, which is surprising to a reader and matters when a provider is configured to accept
	/// exactly one method - a client registered for client_secret_basic will reject this manager's requests,
	/// and vice versa. Changing it would break anyone whose provider is configured for the current method.
	/// </remarks>
	public sealed class TokenExchangeAccessTokenManager : AccessTokenManager<AccessToken>
	{
		private readonly string _clientId;
		private readonly string _clientSecret;
		private readonly string _scopes;
		private readonly ITokenExchangeSubjectTokenSource _subjectTokenSource;

		public const string GrantType = "urn:ietf:params:oauth:grant-type:token-exchange",
							SubjectTokenType = "urn:ietf:params:oauth:token-type:access_token";
		
		private readonly MediaTypeWithQualityHeaderValue _jsonMediaType = new ( "*/*" );

		public TokenExchangeAccessTokenManager( string oAuth2Endpoint, string clientId, string clientSecret, string scopes, 
												ITokenExchangeSubjectTokenSource subjectTokenSource,
												IHttpClientFactory httpClientFactory )
			: this(new Uri( oAuth2Endpoint ?? throw new ArgumentNullException( nameof(oAuth2Endpoint) ) ), 
					clientId, clientSecret, scopes, subjectTokenSource, httpClientFactory)
		{
			
		}
		
		public TokenExchangeAccessTokenManager( Uri oAuth2Endpoint, string clientId, string clientSecret, string scopes, 
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
				IHttpApiClient httpApiClient = new TokenEndpointHttpApiClient( openIdConfiguration.TokenEndpoint, HttpClientFactory );
				
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

			return HandleTokenResponse<AccessToken>(
					responseMessage,
					responseMessage.Content.ReadAsStringAsync().ConfigureAwait( false ).GetAwaiter().GetResult()
				);
		}

		protected override async Task<AccessToken> GetNewAccessTokenAsync()
		{
			OpenIdConfiguration openIdConfiguration = await GetOpenIdConfigurationAsync();
			HttpResponseMessage responseMessage;

			try
			{
				IHttpApiClient httpApiClient = new TokenEndpointHttpApiClient( openIdConfiguration.TokenEndpoint, HttpClientFactory );
				
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

			return HandleTokenResponse<AccessToken>(
					responseMessage,
					await responseMessage.Content.ReadAsStringAsync()
				);
		}
	}
}