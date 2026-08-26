using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Authentication;
using System.Net.Http;
using System.Threading.Tasks;
using Moq;
using Pug.HttpApiClient.OAuth2;
using Pug.HttpApiClient.Tests.Infrastructure;
using Xunit;

namespace Pug.HttpApiClient.Tests.OAuth2
{
	public class RefreshTokenManagerTests
	{
		/// <summary>
		/// Exposes the <c>protected</c> ctor (for a controllable refresh-token source) and the
		/// <c>protected internal</c> <c>GetAccessToken(FormUrlEncodedContent, bool)</c> overload so
		/// tests in this assembly can drive them directly.
		/// </summary>
		private sealed class TestableRefreshTokenManager : RefreshTokenManager
		{
			public TestableRefreshTokenManager( Uri oAuth2Endpoint, string clientId, string clientSecret, string oAuth2Scopes,
												IHttpClientFactory httpClientFactory,
												Func<RefreshTokenManager, RefreshableAccessToken> refreshTokenSource )
				: base( oAuth2Endpoint, clientId, clientSecret, oAuth2Scopes, httpClientFactory, refreshTokenSource )
			{
			}

			public RefreshableAccessToken GetAccessTokenWithCredentialsOption( FormUrlEncodedContent content, bool useClientCredentials ) =>
				GetAccessToken( content, useClientCredentials );

			public RefreshableAccessToken GetAccessTokenWithDefaultCredentialsOption( FormUrlEncodedContent content ) =>
				GetAccessToken( content );
		}

		private static RefreshableAccessToken Seed( string refreshToken = "seed-rt" ) =>
			new () { Token = "seed-token", TokenType = "Bearer", ValidityPeriod = 3600, RefreshToken = refreshToken };

		[Theory]
		[InlineData( null )]
		[InlineData( "" )]
		[InlineData( "   " )]
		public void Constructor_BlankOrNullClientId_ThrowsArgumentException( string clientId )
		{
			ArgumentException exception = Assert.Throws<ArgumentException>(
					() => new RefreshTokenManager( new Uri( "https://auth.example.com" ), clientId, "secret", "scope",
													Seed(), Mock.Of<IHttpClientFactory>() )
				);

			Assert.Equal( "clientId", exception.ParamName );
		}

		[Theory]
		[InlineData( null )]
		[InlineData( "" )]
		[InlineData( "   " )]
		public void Constructor_BlankOrNullClientSecret_ThrowsArgumentException( string clientSecret )
		{
			ArgumentException exception = Assert.Throws<ArgumentException>(
					() => new RefreshTokenManager( new Uri( "https://auth.example.com" ), "client-id", clientSecret, "scope",
													Seed(), Mock.Of<IHttpClientFactory>() )
				);

			Assert.Equal( "clientSecret", exception.ParamName );
		}

		[Theory]
		[InlineData( null )]
		[InlineData( "" )]
		[InlineData( "   " )]
		public void Constructor_BlankOrNullOAuth2Scopes_ThrowsArgumentException( string scopes )
		{
			ArgumentException exception = Assert.Throws<ArgumentException>(
					() => new RefreshTokenManager( new Uri( "https://auth.example.com" ), "client-id", "secret", scopes,
													Seed(), Mock.Of<IHttpClientFactory>() )
				);

			Assert.Equal( "oAuth2Scopes", exception.ParamName );
		}

		[Fact]
		public void Constructor_RefreshTokenSourceReturnsNull_ThrowsAccessTokenException()
		{
			Assert.Throws<AccessTokenException>(
					() => new TestableRefreshTokenManager( new Uri( "https://auth.example.com" ), "client-id", "secret", "scope",
															Mock.Of<IHttpClientFactory>(), _ => null )
				);
		}

		[Fact]
		public void Constructor_PublicOverload_ExposesClientIdAndScopes()
		{
			RefreshTokenManager manager = new ( new Uri( "https://auth.example.com" ), "client-id", "secret", "scope-a scope-b",
												Seed(), Mock.Of<IHttpClientFactory>() );

			Assert.Equal( "client-id", manager.ClientId );
			Assert.Equal( "scope-a scope-b", manager.Scopes );
		}

		[Fact]
		public void GetNewAccessToken_Sync_SendsRefreshTokenGrantWithAllFields()
		{
			OAuth2StubServer stubServer = new ();
			RefreshTokenManager manager = new ( new Uri( stubServer.Issuer ), "client-id", "client-secret", "scope-a",
												Seed(), stubServer.CreateClientFactory() );

			manager.GetAccessToken();

			RecordedRequest tokenRequest = Assert.Single( stubServer.TokenRequests );
			IDictionary<string, string> fields = tokenRequest.FormFields();

			Assert.Equal( "refresh_token", fields["grant_type"] );
			Assert.Equal( "scope-a", fields["scope"] );
			Assert.Equal( "seed-rt", fields["refresh_token"] );
			Assert.Equal( "client-id", fields["client_id"] );
			Assert.Equal( "client-secret", fields["client_secret"] );
		}

		[Fact]
		public async Task GetNewAccessTokenAsync_SendsRefreshTokenGrantWithAllFields()
		{
			OAuth2StubServer stubServer = new ();
			RefreshTokenManager manager = new ( new Uri( stubServer.Issuer ), "client-id", "client-secret", "scope-a",
												Seed(), stubServer.CreateClientFactory() );

			await manager.GetAccessTokenAsync();

			RecordedRequest tokenRequest = Assert.Single( stubServer.TokenRequests );
			IDictionary<string, string> fields = tokenRequest.FormFields();

			Assert.Equal( "refresh_token", fields["grant_type"] );
			Assert.Equal( "scope-a", fields["scope"] );
			Assert.Equal( "seed-rt", fields["refresh_token"] );
			Assert.Equal( "client-id", fields["client_id"] );
			Assert.Equal( "client-secret", fields["client_secret"] );
		}

		[Fact]
		public void GetAccessToken_UseClientCredentialsTrue_AttachesBasicAuthHeader()
		{
			OAuth2StubServer stubServer = new ();
			TestableRefreshTokenManager manager = new ( new Uri( stubServer.Issuer ), "client-id", "client-secret", "scope",
														stubServer.CreateClientFactory(), _ => Seed() );
			FormUrlEncodedContent content = new ( new Dictionary<string, string> { ["grant_type"] = "refresh_token" } );

			manager.GetAccessTokenWithCredentialsOption( content, true );

			RecordedRequest tokenRequest = Assert.Single( stubServer.TokenRequests );

			Assert.Equal( "Basic", tokenRequest.AuthorizationScheme );
			Assert.Equal( Helpers.HttpBase64Encode( "client-id:client-secret" ), tokenRequest.AuthorizationParameter );
		}

		[Fact]
		public void GetAccessToken_UseClientCredentialsFalse_DoesNotAttachBasicAuthHeader()
		{
			OAuth2StubServer stubServer = new ();
			TestableRefreshTokenManager manager = new ( new Uri( stubServer.Issuer ), "client-id", "client-secret", "scope",
														stubServer.CreateClientFactory(), _ => Seed() );
			FormUrlEncodedContent content = new ( new Dictionary<string, string> { ["grant_type"] = "refresh_token" } );

			manager.GetAccessTokenWithCredentialsOption( content, false );

			RecordedRequest tokenRequest = Assert.Single( stubServer.TokenRequests );

			Assert.Null( tokenRequest.AuthorizationScheme );
		}

		[Fact]
		public void GetAccessToken_UseClientCredentialsDefault_DoesNotAttachBasicAuthHeader()
		{
			OAuth2StubServer stubServer = new ();
			TestableRefreshTokenManager manager = new ( new Uri( stubServer.Issuer ), "client-id", "client-secret", "scope",
														stubServer.CreateClientFactory(), _ => Seed() );
			FormUrlEncodedContent content = new ( new Dictionary<string, string> { ["grant_type"] = "refresh_token" } );

			manager.GetAccessTokenWithDefaultCredentialsOption( content );

			RecordedRequest tokenRequest = Assert.Single( stubServer.TokenRequests );

			Assert.Null( tokenRequest.AuthorizationScheme );
		}

		[Fact]
		public void GetAccessToken_Sync_DeserializesRefreshableAccessTokenWithRotatedRefreshToken()
		{
			// KNOWN divergence (documented, not fixed): the sync refresh path deserializes the
			// provider's response as RefreshableAccessToken, so a rotated refresh_token IS present
			// on the object handed back to the caller. Contrast with the async test below.
			OAuth2StubServer stubServer = new ();
			stubServer.AlwaysRespondWith( _ => Responses.Json( OAuth2StubServer.RefreshableTokenJson( refreshToken: "rotated-rt" ) ) );
			RefreshTokenManager manager = new ( new Uri( stubServer.Issuer ), "client-id", "client-secret", "scope",
												Seed(), stubServer.CreateClientFactory() );

			AccessToken result = manager.GetAccessToken();

			RefreshableAccessToken refreshable = Assert.IsType<RefreshableAccessToken>( result );
			Assert.Equal( "rotated-rt", refreshable.RefreshToken );
		}

		[Fact]
		public async Task GetAccessTokenAsync_RefreshGrant_ReturnsRotatedRefreshToken()
		{
			// The async refresh path deserializes RefreshableAccessToken so a rotated refresh_token is
			// visible and can be adopted - without it, async refreshes could never support rotation.
			OAuth2StubServer stubServer = new ();
			stubServer.AlwaysRespondWith( _ => Responses.Json( OAuth2StubServer.RefreshableTokenJson( refreshToken: "rotated-rt" ) ) );
			RefreshTokenManager manager = new ( new Uri( stubServer.Issuer ), "client-id", "client-secret", "scope",
												Seed(), stubServer.CreateClientFactory() );

			AccessToken result = await manager.GetAccessTokenAsync();

			RefreshableAccessToken refreshable = Assert.IsType<RefreshableAccessToken>( result );

			Assert.Equal( "rotated-rt", refreshable.RefreshToken );
		}

		[Fact]
		public void GetAccessToken_AfterRefresh_PresentsRotatedRefreshTokenOnNextRequest()
		{
			// A rotated refresh_token must be adopted: the first refresh presents the seed, and the second
			// must present what the provider handed back rather than replaying the spent seed.
			OAuth2StubServer stubServer = new ();
			stubServer.EnqueueTokenResponse( _ => Responses.Json( OAuth2StubServer.RefreshableTokenJson( refreshToken: "rotated-rt-1", expiresIn: 1 ) ) );
			stubServer.EnqueueTokenResponse( _ => Responses.Json( OAuth2StubServer.RefreshableTokenJson( refreshToken: "rotated-rt-2", expiresIn: 1 ) ) );
			RefreshTokenManager manager = new ( new Uri( stubServer.Issuer ), "client-id", "client-secret", "scope",
												Seed(), stubServer.CreateClientFactory() );

			manager.GetAccessToken();
			manager.GetAccessToken();

			Assert.Equal( 2, stubServer.TokenRequests.Count );
			Assert.Equal( "seed-rt", stubServer.TokenRequests[0].FormFields()["refresh_token"] );
			Assert.Equal( "rotated-rt-1", stubServer.TokenRequests[1].FormFields()["refresh_token"] );
		}

		[Fact]
		public async Task GetAccessTokenAsync_RefreshGrant_SendsNoBasicAuthHeader_CredentialsTravelInTheBody()
		{
			// The async refresh grant sends no Basic auth header, because RefreshTokenManager's refresh
			// grant carries client_id/client_secret as FORM FIELDS (client_secret_post) - see
			// GetNewAccessTokenAsync. The useClientCredentials parameter exists only on the sync overload
			// because that overload also serves the constructor-time INITIAL grant, which does use
			// client_secret_basic; there is no async initial-grant path because those grants are issued
			// from constructors, which cannot await. So this is a property of the refresh grant, not a
			// gap in the async method.
			OAuth2StubServer stubServer = new ();
			RefreshTokenManager manager = new ( new Uri( stubServer.Issuer ), "client-id", "client-secret", "scope",
												Seed(), stubServer.CreateClientFactory() );

			await manager.GetAccessTokenAsync();

			RecordedRequest tokenRequest = Assert.Single( stubServer.TokenRequests );

			Assert.Null( tokenRequest.AuthorizationScheme );
		}
		[Fact]
		public void GetAccessToken_ProviderRotatesRefreshTokens_SubsequentRefreshesSucceed()
		{
			// The stub models single-use refresh tokens - Keycloak's "Revoke Refresh Token", and the
			// behaviour RFC 6749 section 6 permits: each success issues a NEW refresh token and any token
			// presented twice is rejected with invalid_grant. This is the strictest provider policy, and the
			// manager must survive it across repeated refreshes.
			//
			// The ordinary stub cannot catch a replay because it answers 200 to any refresh_token at all -
			// which is exactly why this scenario needs its own rotation-aware responder.
			HashSet<string> spentRefreshTokens = new ();
			int issued = 0;

			OAuth2StubServer stubServer = new ();
			stubServer.AlwaysRespondWith( request =>
				{
					string presented = request.FormFields()["refresh_token"];

					if( !spentRefreshTokens.Add( presented ) )
						return Responses.Create(
								HttpStatusCode.BadRequest,
								OAuth2StubServer.ErrorJson( "invalid_grant", "Token is not active" ) );

					issued++;

					return Responses.Json(
							OAuth2StubServer.RefreshableTokenJson(
									token: $"access-{issued}", refreshToken: $"rotated-rt-{issued}", expiresIn: 1 ) );
				} );

			RefreshTokenManager manager = new ( new Uri( stubServer.Issuer ), "client-id", "client-secret", "scope",
												Seed(), stubServer.CreateClientFactory() );

			// first refresh spends "seed-rt" and the provider hands back "rotated-rt-1"
			AccessToken first = manager.GetAccessToken();

			Assert.Equal( "access-1", first.Token );

			// expires_in 1 (minus the 5s margin) forces a genuine second and third fetch
			AccessToken second = manager.GetAccessToken();
			AccessToken third = manager.GetAccessToken();

			Assert.Equal( "access-2", second.Token );
			Assert.Equal( "access-3", third.Token );

			// each request presents the token issued by the one before it, never a spent one
			Assert.Equal( "seed-rt", stubServer.TokenRequests[0].FormFields()["refresh_token"] );
			Assert.Equal( "rotated-rt-1", stubServer.TokenRequests[1].FormFields()["refresh_token"] );
			Assert.Equal( "rotated-rt-2", stubServer.TokenRequests[2].FormFields()["refresh_token"] );
		}
	}
}