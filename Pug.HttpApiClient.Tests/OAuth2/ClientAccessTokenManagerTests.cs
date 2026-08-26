using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading.Tasks;
using Moq;
using Pug.HttpApiClient.OAuth2;
using Pug.HttpApiClient.Tests.Infrastructure;
using Xunit;

namespace Pug.HttpApiClient.Tests.OAuth2
{
	public class ClientAccessTokenManagerTests
	{
		[Theory]
		[InlineData( null )]
		[InlineData( "" )]
		[InlineData( "   " )]
		public void Constructor_BlankOrNullClientId_ThrowsArgumentException( string clientId )
		{
			ArgumentException exception = Assert.Throws<ArgumentException>(
					() => new ClientAccessTokenManager( new Uri( "https://auth.example.com" ), clientId, "secret", "scope",
															Mock.Of<IHttpClientFactory>() )
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
					() => new ClientAccessTokenManager( new Uri( "https://auth.example.com" ), "client-id", clientSecret, "scope",
															Mock.Of<IHttpClientFactory>() )
				);

			Assert.Equal( "clientSecret", exception.ParamName );
		}

		[Fact]
		public void Constructor_NullScopes_ThrowsArgumentNullException()
		{
			ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
					() => new ClientAccessTokenManager( new Uri( "https://auth.example.com" ), "client-id", "secret", null,
															Mock.Of<IHttpClientFactory>() )
				);

			Assert.Equal( "scopes", exception.ParamName );
		}

		[Fact]
		public void Constructor_ValidArguments_ExposesClientId()
		{
			ClientAccessTokenManager manager = new ( new Uri( "https://auth.example.com" ), "client-id", "secret", "scope",
														Mock.Of<IHttpClientFactory>() );

			Assert.Equal( "client-id", manager.ClientId );
		}

		[Fact]
		public void GetAccessToken_Sync_RequestsDiscoveryBeforeToken()
		{
			OAuth2StubServer stubServer = new ();

			stubServer.EnqueueTokenResponse( _ =>
				{
					// Discovery must have already completed by the time the token endpoint is reached.
					Assert.Single( stubServer.DiscoveryRequests );

					return Responses.Json( OAuth2StubServer.AccessTokenJson() );
				} );

			ClientAccessTokenManager manager = new ( new Uri( stubServer.Issuer ), "client-id", "secret", "scope",
														stubServer.CreateClientFactory() );

			manager.GetAccessToken();

			Assert.Single( stubServer.DiscoveryRequests );
			Assert.Single( stubServer.TokenRequests );
		}

		[Fact]
		public async Task GetAccessTokenAsync_RequestsDiscoveryBeforeToken()
		{
			OAuth2StubServer stubServer = new ();

			stubServer.EnqueueTokenResponse( _ =>
				{
					Assert.Single( stubServer.DiscoveryRequests );

					return Responses.Json( OAuth2StubServer.AccessTokenJson() );
				} );

			ClientAccessTokenManager manager = new ( new Uri( stubServer.Issuer ), "client-id", "secret", "scope",
														stubServer.CreateClientFactory() );

			await manager.GetAccessTokenAsync();

			Assert.Single( stubServer.DiscoveryRequests );
			Assert.Single( stubServer.TokenRequests );
		}

		[Fact]
		public void GetAccessToken_Sync_SendsClientCredentialsGrantWithScopeAndBasicAuth()
		{
			OAuth2StubServer stubServer = new ();
			ClientAccessTokenManager manager = new ( new Uri( stubServer.Issuer ), "client-id", "s3cr3t", "scope-a scope-b",
														stubServer.CreateClientFactory() );

			manager.GetAccessToken();

			RecordedRequest tokenRequest = Assert.Single( stubServer.TokenRequests );
			IDictionary<string, string> fields = tokenRequest.FormFields();

			Assert.Equal( "client_credentials", fields["grant_type"] );
			Assert.Equal( "scope-a scope-b", fields["scope"] );
			Assert.Equal( "Basic", tokenRequest.AuthorizationScheme );
			Assert.Equal( Helpers.HttpBase64Encode( "client-id:s3cr3t" ), tokenRequest.AuthorizationParameter );
		}

		[Fact]
		public async Task GetAccessTokenAsync_SendsClientCredentialsGrantWithScopeAndBasicAuth()
		{
			OAuth2StubServer stubServer = new ();
			ClientAccessTokenManager manager = new ( new Uri( stubServer.Issuer ), "client-id", "s3cr3t", "scope-a scope-b",
														stubServer.CreateClientFactory() );

			await manager.GetAccessTokenAsync();

			RecordedRequest tokenRequest = Assert.Single( stubServer.TokenRequests );
			IDictionary<string, string> fields = tokenRequest.FormFields();

			Assert.Equal( "client_credentials", fields["grant_type"] );
			Assert.Equal( "scope-a scope-b", fields["scope"] );
			Assert.Equal( "Basic", tokenRequest.AuthorizationScheme );
			Assert.Equal( Helpers.HttpBase64Encode( "client-id:s3cr3t" ), tokenRequest.AuthorizationParameter );
		}

		[Fact]
		public void GetAccessToken_Sync_ReturnsTokenPopulatedFromResponse()
		{
			OAuth2StubServer stubServer = new ();
			stubServer.AlwaysRespondWith( _ => Responses.Json( OAuth2StubServer.AccessTokenJson( "tok-123", "Bearer", 3600 ) ) );
			ClientAccessTokenManager manager = new ( new Uri( stubServer.Issuer ), "client-id", "secret", "scope",
														stubServer.CreateClientFactory() );

			AccessToken token = manager.GetAccessToken();

			Assert.Equal( "tok-123", token.Token );
			Assert.Equal( "Bearer", token.TokenType );
			Assert.Equal( 3600, token.ValidityPeriod );
		}

		[Fact]
		public async Task GetAccessTokenAsync_ReturnsTokenPopulatedFromResponse()
		{
			OAuth2StubServer stubServer = new ();
			stubServer.AlwaysRespondWith( _ => Responses.Json( OAuth2StubServer.AccessTokenJson( "tok-async-123", "Bearer", 3600 ) ) );
			ClientAccessTokenManager manager = new ( new Uri( stubServer.Issuer ), "client-id", "secret", "scope",
														stubServer.CreateClientFactory() );

			AccessToken token = await manager.GetAccessTokenAsync();

			Assert.Equal( "tok-async-123", token.Token );
			Assert.Equal( "Bearer", token.TokenType );
			Assert.Equal( 3600, token.ValidityPeriod );
		}

		[Fact]
		public void GetAccessToken_ProviderReturns400WithErrorAndDescription_ThrowsAuthenticationExceptionWithCombinedMessage()
		{
			OAuth2StubServer stubServer = new ();
			stubServer.AlwaysRespondWith( HttpStatusCode.BadRequest, OAuth2StubServer.ErrorJson( "invalid_client", "bad secret" ) );
			ClientAccessTokenManager manager = new ( new Uri( stubServer.Issuer ), "client-id", "secret", "scope",
														stubServer.CreateClientFactory() );

			AuthenticationException exception = Assert.Throws<AuthenticationException>( () => manager.GetAccessToken() );

			Assert.Equal( "invalid_client: bad secret", exception.Message );
		}

		[Fact]
		public void GetAccessToken_ProviderReturns401WithErrorAndDescription_ThrowsSameAuthenticationExceptionAs400()
		{
			// Fix C: real providers that authenticate the client via HTTP Basic MAY answer 401
			// rather than 400 for bad client credentials - both must map identically.
			OAuth2StubServer stubServer = new ();
			stubServer.AlwaysRespondWith( HttpStatusCode.Unauthorized, OAuth2StubServer.ErrorJson( "invalid_client", "bad secret" ) );
			ClientAccessTokenManager manager = new ( new Uri( stubServer.Issuer ), "client-id", "secret", "scope",
														stubServer.CreateClientFactory() );

			AuthenticationException exception = Assert.Throws<AuthenticationException>( () => manager.GetAccessToken() );

			Assert.Equal( "invalid_client: bad secret", exception.Message );
		}

		[Fact]
		public void GetAccessToken_ProviderReturns400WithErrorOnly_ThrowsAuthenticationExceptionWithErrorAsMessage()
		{
			OAuth2StubServer stubServer = new ();
			stubServer.AlwaysRespondWith( HttpStatusCode.BadRequest, OAuth2StubServer.ErrorJson( "invalid_client" ) );
			ClientAccessTokenManager manager = new ( new Uri( stubServer.Issuer ), "client-id", "secret", "scope",
														stubServer.CreateClientFactory() );

			AuthenticationException exception = Assert.Throws<AuthenticationException>( () => manager.GetAccessToken() );

			Assert.Equal( "invalid_client", exception.Message );
		}

		[Fact]
		public void GetAccessToken_ProviderReturns400WithNonJsonBody_ThrowsHttpApiRequestExceptionNotJsonException()
		{
			OAuth2StubServer stubServer = new ();
			stubServer.AlwaysRespondWith( HttpStatusCode.BadRequest, "<html>oops</html>" );
			ClientAccessTokenManager manager = new ( new Uri( stubServer.Issuer ), "client-id", "secret", "scope",
														stubServer.CreateClientFactory() );

			HttpApiRequestException exception = Assert.Throws<HttpApiRequestException>( () => manager.GetAccessToken() );

			Assert.Equal( HttpStatusCode.BadRequest, exception.ResponseStatusCode );
		}

		[Fact]
		public void GetAccessToken_ProviderReturns500_ThrowsHttpApiRequestExceptionWithUnexpectedStatusMessage()
		{
			OAuth2StubServer stubServer = new ();
			stubServer.AlwaysRespondWith( HttpStatusCode.InternalServerError, "" );
			ClientAccessTokenManager manager = new ( new Uri( stubServer.Issuer ), "client-id", "secret", "scope",
														stubServer.CreateClientFactory() );

			HttpApiRequestException exception = Assert.Throws<HttpApiRequestException>( () => manager.GetAccessToken() );

			Assert.Equal( "Unexpected response status code received from OAuth2 provider: 500", exception.Message );
		}

		[Fact]
		public async Task GetAccessTokenAsync_ProviderReturns500_ThrowsHttpApiRequestExceptionWithUnexpectedStatusMessage()
		{
			// Fix #4: before the fix, the async path let a bare HttpRequestException escape instead
			// of the same HttpApiRequestException the sync path throws.
			OAuth2StubServer stubServer = new ();
			stubServer.AlwaysRespondWith( HttpStatusCode.InternalServerError, "" );
			ClientAccessTokenManager manager = new ( new Uri( stubServer.Issuer ), "client-id", "secret", "scope",
														stubServer.CreateClientFactory() );

			HttpApiRequestException exception =
				await Assert.ThrowsAsync<HttpApiRequestException>( () => manager.GetAccessTokenAsync() );

			Assert.Equal( "Unexpected response status code received from OAuth2 provider: 500", exception.Message );
		}

		[Fact]
		public void GetAccessToken_ProviderReturnsUnexpected2xx_ThrowsHttpApiRequestExceptionWithUnexpectedStatusMessage()
		{
			OAuth2StubServer stubServer = new ();
			stubServer.AlwaysRespondWith( HttpStatusCode.NoContent, "" );
			ClientAccessTokenManager manager = new ( new Uri( stubServer.Issuer ), "client-id", "secret", "scope",
														stubServer.CreateClientFactory() );

			HttpApiRequestException exception = Assert.Throws<HttpApiRequestException>( () => manager.GetAccessToken() );

			Assert.Equal( "Unexpected response status code received from OAuth2 provider: 204", exception.Message );
		}

		[Fact]
		public void GetAccessToken_Sync_TokenAlreadyExpiredOnReturn_SecondCallRefetchesFromProvider()
		{
			// expires_in: 1 minus the manager's 5s safety margin means the token is already expired the
			// instant it is returned, so the second GetAccessToken() call must trigger a genuine re-fetch
			// rather than serving from cache.
			//
			// NOTE: this was originally written to guard a supposed ObjectDisposedException from the
			// manager caching one FormUrlEncodedContent across requests. Mutation testing disproved that:
			// re-introducing the cached instance breaks nothing, here OR against real Keycloak, because
			// FormUrlEncodedContent derives from ByteArrayContent and its byte[] survives disposal, so it
			// is safely re-sendable. The "never reuse HttpContent" rule bites stream-backed content, not
			// this. The test still earns its place as coverage of the expiry/re-fetch path.
			OAuth2StubServer stubServer = new ();
			stubServer.AlwaysRespondWith( _ => Responses.Json( OAuth2StubServer.AccessTokenJson( expiresIn: 1 ) ) );
			ClientAccessTokenManager manager = new ( new Uri( stubServer.Issuer ), "client-id", "secret", "scope",
														stubServer.CreateClientFactory() );

			AccessToken first = manager.GetAccessToken();
			AccessToken second = manager.GetAccessToken();

			Assert.NotNull( first );
			Assert.NotNull( second );
			Assert.Equal( 2, stubServer.TokenRequests.Count );
		}

		[Fact]
		public async Task GetAccessTokenAsync_TokenAlreadyExpiredOnReturn_SecondCallRefetchesFromProvider()
		{
			OAuth2StubServer stubServer = new ();
			stubServer.AlwaysRespondWith( _ => Responses.Json( OAuth2StubServer.AccessTokenJson( expiresIn: 1 ) ) );
			ClientAccessTokenManager manager = new ( new Uri( stubServer.Issuer ), "client-id", "secret", "scope",
														stubServer.CreateClientFactory() );

			AccessToken first = await manager.GetAccessTokenAsync();
			AccessToken second = await manager.GetAccessTokenAsync();

			Assert.NotNull( first );
			Assert.NotNull( second );
			Assert.Equal( 2, stubServer.TokenRequests.Count );
		}
	}
}
