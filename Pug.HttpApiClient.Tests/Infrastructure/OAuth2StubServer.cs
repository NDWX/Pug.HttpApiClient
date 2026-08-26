using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace Pug.HttpApiClient.Tests.Infrastructure
{
	/// <summary>
	/// Minimal OAuth2 provider stub: serves an OpenID discovery document and a token endpoint.
	/// </summary>
	/// <remarks>
	/// Needed because every <c>AccessTokenManager</c> constructs its own <c>HttpApiClient</c> internally for
	/// both the discovery hop and the token hop - there is no injection seam, so the only way to observe or
	/// steer them is at the transport.
	/// </remarks>
	public sealed class OAuth2StubServer
	{
		public const string DiscoveryPath = "/.well-known/openid-configuration";

		private readonly List<Func<RecordedRequest, HttpResponseMessage>> _tokenResponses = new ();
		private Func<RecordedRequest, HttpResponseMessage> _defaultTokenResponse;

		public OAuth2StubServer( string issuer = "https://auth.example.com/realms/acme" )
		{
			Issuer = issuer.TrimEnd( '/' );
			TokenEndpoint = $"{Issuer}/protocol/openid-connect/token";
			_defaultTokenResponse = _ => Responses.Json( AccessTokenJson() );
		}

		public string Issuer { get; }

		public string TokenEndpoint { get; set; }

		/// <summary>Requests that reached the discovery endpoint.</summary>
		public List<RecordedRequest> DiscoveryRequests { get; } = new ();

		/// <summary>Requests that reached the token endpoint.</summary>
		public List<RecordedRequest> TokenRequests { get; } = new ();

		/// <summary>Answer the next token request with this response; queued responses are consumed in order.</summary>
		public OAuth2StubServer EnqueueTokenResponse( Func<RecordedRequest, HttpResponseMessage> response )
		{
			_tokenResponses.Add( response );
			return this;
		}

		public OAuth2StubServer EnqueueTokenResponse( HttpStatusCode statusCode, string body ) =>
			EnqueueTokenResponse( _ => Responses.Create( statusCode, body ) );

		/// <summary>Answer every otherwise-unqueued token request with this response.</summary>
		public OAuth2StubServer AlwaysRespondWith( Func<RecordedRequest, HttpResponseMessage> response )
		{
			_defaultTokenResponse = response;
			return this;
		}

		public OAuth2StubServer AlwaysRespondWith( HttpStatusCode statusCode, string body ) =>
			AlwaysRespondWith( _ => Responses.Create( statusCode, body ) );

		public StubHttpMessageHandler CreateHandler() => new ( Respond );

		public StubHttpClientFactory CreateClientFactory() => new ( CreateHandler() );

		private HttpResponseMessage Respond( RecordedRequest request )
		{
			if( request.AbsolutePath.EndsWith( DiscoveryPath, StringComparison.Ordinal ) )
			{
				DiscoveryRequests.Add( request );
				return Responses.Json( DiscoveryJson() );
			}

			TokenRequests.Add( request );

			if( _tokenResponses.Count > 0 )
			{
				Func<RecordedRequest, HttpResponseMessage> next = _tokenResponses[0];
				_tokenResponses.RemoveAt( 0 );
				return next( request );
			}

			return _defaultTokenResponse( request );
		}

		public string DiscoveryJson() =>
			$@"{{
				""issuer"": ""{Issuer}"",
				""authorization_endpoint"": ""{Issuer}/protocol/openid-connect/auth"",
				""token_endpoint"": ""{TokenEndpoint}"",
				""jwks_uri"": ""{Issuer}/protocol/openid-connect/certs"",
				""scopes_supported"": [ ""openid"", ""profile"" ]
			}}";

		public static string AccessTokenJson( string token = "access-token-value", string tokenType = "Bearer",
												int expiresIn = 3600 ) =>
			$@"{{ ""access_token"": ""{token}"", ""token_type"": ""{tokenType}"", ""expires_in"": {expiresIn} }}";

		public static string RefreshableTokenJson( string token = "access-token-value", string refreshToken = "refresh-token-value",
													string tokenType = "Bearer", int expiresIn = 3600 ) =>
			$@"{{ ""access_token"": ""{token}"", ""token_type"": ""{tokenType}"", ""expires_in"": {expiresIn},
				""refresh_token"": ""{refreshToken}"" }}";

		public static string ErrorJson( string error, string description = null ) =>
			description is null
				? $@"{{ ""error"": ""{error}"" }}"
				: $@"{{ ""error"": ""{error}"", ""error_description"": ""{description}"" }}";
	}
}
