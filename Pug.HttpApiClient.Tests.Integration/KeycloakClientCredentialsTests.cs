using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Pug.HttpApiClient.OAuth2;
using Xunit;
using Xunit.Abstractions;

namespace Pug.HttpApiClient.Tests.Integration
{
	[Collection( KeycloakCollection.Name )]
	[Trait( "Category", "Integration" )]
	public class KeycloakClientCredentialsTests
	{
		private readonly KeycloakFixture _keycloak;
		private readonly ITestOutputHelper _output;

		public KeycloakClientCredentialsTests( KeycloakFixture keycloak, ITestOutputHelper output )
		{
			_keycloak = keycloak;
			_output = output;
		}

		[SkippableFact]
		public async Task GetAccessTokenAsync_ApiClientCredentials_ReturnsTokenWithMatchingIssuerAndAzp()
		{
			Skip.If( _keycloak.SkipReason is not null, _keycloak.SkipReason );

			ClientAccessTokenManager manager = new (
					new Uri( _keycloak.Issuer ), "api-client", "api-secret", "openid",
					_keycloak.HttpClientFactory );

			AccessToken token = await manager.GetAccessTokenAsync();

			JsonElement payload = JwtTestHelper.DecodePayload( token.Token );

			Assert.Equal( _keycloak.Issuer, payload.GetProperty( "iss" ).GetString() );

			string clientIdentity = payload.TryGetProperty( "azp", out JsonElement azp )
					? azp.GetString()
					: payload.GetProperty( "client_id" ).GetString();

			Assert.Equal( "api-client", clientIdentity );
		}

		[SkippableFact]
		public async Task GetAccessTokenAsync_WrongClientSecret_ThrowsAuthenticationExceptionWithProviderMessage()
		{
			Skip.If( _keycloak.SkipReason is not null, _keycloak.SkipReason );

			ClientAccessTokenManager manager = new (
					new Uri( _keycloak.Issuer ), "api-client", "wrong-secret", "openid",
					_keycloak.HttpClientFactory );

			AuthenticationException exception =
					await Assert.ThrowsAsync<AuthenticationException>( () => manager.GetAccessTokenAsync() );

			// Observed against real Keycloak 26: it answers a bad client secret with error
			// "unauthorized_client" (not the RFC 6749 example "invalid_client"), with description
			// "Invalid client or Invalid client credentials".
			Assert.Contains( "unauthorized_client", exception.Message );
		}

		[SkippableFact]
		public async Task RawTokenRequest_WrongClientSecret_ReportsActualHttpStatus()
		{
			Skip.If( _keycloak.SkipReason is not null, _keycloak.SkipReason );

			using HttpClient httpClient = _keycloak.HttpClientFactory.CreateClient();

			using HttpRequestMessage request = new ( HttpMethod.Post, $"{_keycloak.Issuer}/protocol/openid-connect/token" )
			{
				Content = new FormUrlEncodedContent( new Dictionary<string, string>
				{
					["grant_type"] = "client_credentials",
					["scope"] = "openid"
				} )
			};

			request.Headers.Authorization = new AuthenticationHeaderValue(
					"Basic", Convert.ToBase64String( Encoding.UTF8.GetBytes( "api-client:wrong-secret" ) ) );

			HttpResponseMessage response = await httpClient.SendAsync( request );
			string body = await response.Content.ReadAsStringAsync();

			_output.WriteLine( $"Keycloak status for bad client secret: {(int)response.StatusCode} ({response.StatusCode}). Body: {body}" );

			// The real point of this test: pin down which status Keycloak actually uses for bad client
			// credentials. RFC 6749 says 400; a provider authenticating the client via HTTP Basic MAY use
			// 401 instead - Fix C exists precisely because a real 401 used to produce a message-less
			// AuthenticationException when only 400 was handled.
			Assert.True(
					response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized,
					$"Expected 400 or 401, got {(int)response.StatusCode} ({response.StatusCode}). Body: {body}" );

			Assert.Contains( "unauthorized_client", body );
		}

		[SkippableFact]
		public void GetAccessToken_ShortLivedClientCalledTwiceSync_BothCallsReturnRealTokens()
		{
			Skip.If( _keycloak.SkipReason is not null, _keycloak.SkipReason );

			ClientAccessTokenManager manager = new (
					new Uri( _keycloak.Issuer ), "short-lived-client", "short-secret", "openid",
					_keycloak.HttpClientFactory );

			// access.token.lifespan is 1 second and the manager's safety margin is 5 seconds, so every
			// token is deemed pre-expired: this forces a fresh token fetch on every single call, including
			// the second one - exactly the case that threw ObjectDisposedException before Fix B.
			AccessToken first = manager.GetAccessToken();
			AccessToken second = manager.GetAccessToken();

			Assert.False( string.IsNullOrWhiteSpace( first.Token ) );
			Assert.Equal( 3, first.Token.Split( '.' ).Length );
			Assert.False( string.IsNullOrWhiteSpace( second.Token ) );
			Assert.Equal( 3, second.Token.Split( '.' ).Length );
		}

		[SkippableFact]
		public async Task GetAccessTokenAsync_ShortLivedClientCalledTwiceAsync_BothCallsReturnRealTokens()
		{
			Skip.If( _keycloak.SkipReason is not null, _keycloak.SkipReason );

			ClientAccessTokenManager manager = new (
					new Uri( _keycloak.Issuer ), "short-lived-client", "short-secret", "openid",
					_keycloak.HttpClientFactory );

			AccessToken first = await manager.GetAccessTokenAsync();
			AccessToken second = await manager.GetAccessTokenAsync();

			Assert.False( string.IsNullOrWhiteSpace( first.Token ) );
			Assert.Equal( 3, first.Token.Split( '.' ).Length );
			Assert.False( string.IsNullOrWhiteSpace( second.Token ) );
			Assert.Equal( 3, second.Token.Split( '.' ).Length );
		}
	}
}
