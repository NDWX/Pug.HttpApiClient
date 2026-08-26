using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using Pug.HttpApiClient.OAuth2;
using Xunit;
using Xunit.Abstractions;

namespace Pug.HttpApiClient.Tests.Integration
{
	[Collection( KeycloakCollection.Name )]
	[Trait( "Category", "Integration" )]
	public class KeycloakPasswordAndRefreshTests
	{
		private readonly KeycloakFixture _keycloak;
		private readonly ITestOutputHelper _output;

		public KeycloakPasswordAndRefreshTests( KeycloakFixture keycloak, ITestOutputHelper output )
		{
			_keycloak = keycloak;
			_output = output;
		}

		// PasswordAccessTokenManager is sealed, so it cannot be subclassed to expose the seeded token, and
		// its public GetAccessToken() would itself perform an extra refresh-grant call on first use (the
		// base class's token cache starts empty) - conflating "password grant returned a token" (this
		// class) with "refresh works" (below). Reading the protected RefreshTokenManager.AccessToken
		// property via reflection captures the actual password-grant response without an extra request.
		private static RefreshableAccessToken GetSeedToken( RefreshTokenManager manager )
		{
			PropertyInfo property = typeof(RefreshTokenManager).GetProperty(
					"AccessToken", BindingFlags.NonPublic | BindingFlags.Instance );

			return (RefreshableAccessToken)property.GetValue( manager );
		}

		[SkippableFact]
		public void Constructor_PasswordClientWithValidCredentials_SeedsRealRefreshableAccessToken()
		{
			Skip.If( _keycloak.SkipReason is not null, _keycloak.SkipReason );

			PasswordAccessTokenManager manager = new (
					new Uri( _keycloak.Issuer ), "password-client", "password-secret",
					"alice", "alice-password", "openid", _keycloak.HttpClientFactory );

			RefreshableAccessToken seedToken = GetSeedToken( manager );

			Assert.False( string.IsNullOrWhiteSpace( seedToken.Token ) );
			Assert.Equal( 3, seedToken.Token.Split( '.' ).Length );
			Assert.False( string.IsNullOrWhiteSpace( seedToken.RefreshToken ) );
		}

		[SkippableFact]
		public void GetAccessToken_RefreshTokenManagerSeededFromPasswordFlow_ReturnsNewRealToken()
		{
			Skip.If( _keycloak.SkipReason is not null, _keycloak.SkipReason );

			PasswordAccessTokenManager passwordManager = new (
					new Uri( _keycloak.Issuer ), "password-client", "password-secret",
					"alice", "alice-password", "openid", _keycloak.HttpClientFactory );

			RefreshableAccessToken seedToken = GetSeedToken( passwordManager );

			RefreshTokenManager refreshManager = new (
					new Uri( _keycloak.Issuer ), "password-client", "password-secret", "openid",
					seedToken, _keycloak.HttpClientFactory );

			AccessToken refreshed = refreshManager.GetAccessToken();

			Assert.False( string.IsNullOrWhiteSpace( refreshed.Token ) );
			Assert.Equal( 3, refreshed.Token.Split( '.' ).Length );
			Assert.NotEqual( seedToken.Token, refreshed.Token );
		}

		[SkippableFact]
		public void Constructor_PasswordClientWithWrongPassword_ThrowsAuthenticationExceptionWithInvalidGrant()
		{
			Skip.If( _keycloak.SkipReason is not null, _keycloak.SkipReason );

			AuthenticationException exception = Assert.Throws<AuthenticationException>( () =>
					new PasswordAccessTokenManager(
							new Uri( _keycloak.Issuer ), "password-client", "password-secret",
							"alice", "wrong-password", "openid", _keycloak.HttpClientFactory ) );

			Assert.Contains( "invalid_grant", exception.Message );
		}

		[SkippableFact]
		public async Task RawTokenRequest_WrongPassword_ReportsActualHttpStatus()
		{
			Skip.If( _keycloak.SkipReason is not null, _keycloak.SkipReason );

			using HttpClient httpClient = _keycloak.HttpClientFactory.CreateClient();

			using HttpRequestMessage request = new ( HttpMethod.Post, $"{_keycloak.Issuer}/protocol/openid-connect/token" )
			{
				Content = new FormUrlEncodedContent( new Dictionary<string, string>
				{
					["grant_type"] = "password",
					["scope"] = "openid",
					["username"] = "alice",
					["password"] = "wrong-password"
				} )
			};

			request.Headers.Authorization = new AuthenticationHeaderValue(
					"Basic", Convert.ToBase64String( Encoding.UTF8.GetBytes( "password-client:password-secret" ) ) );

			HttpResponseMessage response = await httpClient.SendAsync( request );
			string body = await response.Content.ReadAsStringAsync();

			_output.WriteLine( $"Keycloak status for bad user credentials: {(int)response.StatusCode} ({response.StatusCode}). Body: {body}" );

			Assert.True(
					response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized,
					$"Expected 400 or 401, got {(int)response.StatusCode} ({response.StatusCode}). Body: {body}" );

			Assert.Contains( "invalid_grant", body );
		}
		[SkippableFact]
		public void GetAccessToken_RealKeycloakRotatingRefreshTokens_SubsequentRefreshesSucceed()
		{
			Skip.If( _keycloak.SkipReason is not null, _keycloak.SkipReason );

			// The realm sets revokeRefreshToken/refreshTokenMaxReuse 0, so Keycloak issues a NEW refresh
			// token on every refresh and rejects any token presented twice. rotating-client's access tokens
			// live 1s, which is inside the manager's 5s expiry margin, so the second GetAccessToken() is a
			// genuine refresh rather than a cache hit.
			//
			// RefreshTokenManager adopts the refresh token the provider returns, so each refresh presents the
			// current token instead of replaying the spent seed. Before that fix, real Keycloak rejected the
			// second refresh with "invalid_grant: Maximum allowed refresh token reuse exceeded".
			PasswordAccessTokenManager passwordManager = new (
					new Uri( _keycloak.Issuer ), "rotating-client", "rotating-secret",
					"alice", "alice-password", "openid", _keycloak.HttpClientFactory );

			RefreshableAccessToken seedToken = GetSeedToken( passwordManager );

			RefreshTokenManager refreshManager = new (
					new Uri( _keycloak.Issuer ), "rotating-client", "rotating-secret", "openid",
					seedToken, _keycloak.HttpClientFactory );

			AccessToken first = refreshManager.GetAccessToken();
			AccessToken second = refreshManager.GetAccessToken();
			AccessToken third = refreshManager.GetAccessToken();

			// three consecutive refreshes against a server that revokes a refresh token the moment it is used
			foreach( AccessToken token in new[] { first, second, third } )
			{
				Assert.False( string.IsNullOrWhiteSpace( token.Token ) );
				Assert.Equal( 3, token.Token.Split( '.' ).Length );
			}

			Assert.NotEqual( first.Token, second.Token );
			Assert.NotEqual( second.Token, third.Token );

			_output.WriteLine( "three consecutive refreshes accepted by real Keycloak with rotation enabled" );
		}
	}
}