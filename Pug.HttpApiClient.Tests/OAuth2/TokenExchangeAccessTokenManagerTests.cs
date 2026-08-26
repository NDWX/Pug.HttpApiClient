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
	public class TokenExchangeAccessTokenManagerTests
	{
		[Fact]
		public void Constructor_StringEndpointNull_ThrowsArgumentNullException()
		{
			ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
					() => new TokenExchangeAccessTokenManager( (string)null, "client-id", "secret", "scope",
																Mock.Of<ITokenExchangeSubjectTokenSource>(), Mock.Of<IHttpClientFactory>() )
				);

			Assert.Equal( "oAuth2Endpoint", exception.ParamName );
		}

		[Theory]
		[InlineData( null )]
		[InlineData( "" )]
		[InlineData( "   " )]
		public void Constructor_BlankOrNullClientId_ThrowsArgumentException( string clientId )
		{
			ArgumentException exception = Assert.Throws<ArgumentException>(
					() => new TokenExchangeAccessTokenManager( new Uri( "https://auth.example.com" ), clientId, "secret", "scope",
																Mock.Of<ITokenExchangeSubjectTokenSource>(), Mock.Of<IHttpClientFactory>() )
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
					() => new TokenExchangeAccessTokenManager( new Uri( "https://auth.example.com" ), "client-id", clientSecret, "scope",
																Mock.Of<ITokenExchangeSubjectTokenSource>(), Mock.Of<IHttpClientFactory>() )
				);

			Assert.Equal( "clientSecret", exception.ParamName );
		}

		[Theory]
		[InlineData( null )]
		[InlineData( "" )]
		[InlineData( "   " )]
		public void Constructor_BlankOrNullScopes_ThrowsArgumentException( string scopes )
		{
			ArgumentException exception = Assert.Throws<ArgumentException>(
					() => new TokenExchangeAccessTokenManager( new Uri( "https://auth.example.com" ), "client-id", "secret", scopes,
																Mock.Of<ITokenExchangeSubjectTokenSource>(), Mock.Of<IHttpClientFactory>() )
				);

			Assert.Equal( "scopes", exception.ParamName );
		}

		[Fact]
		public void Constructor_NullSubjectTokenSource_ThrowsArgumentNullException()
		{
			ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
					() => new TokenExchangeAccessTokenManager( new Uri( "https://auth.example.com" ), "client-id", "secret", "scope",
																null, Mock.Of<IHttpClientFactory>() )
				);

			Assert.Equal( "subjectTokenSource", exception.ParamName );
		}

		[Fact]
		public void GetNewAccessToken_Sync_SendsTokenExchangeGrantWithSubjectTokenFromSource()
		{
			// Exercises the string-endpoint ctor overload.
			OAuth2StubServer stubServer = new ();
			Mock<ITokenExchangeSubjectTokenSource> subjectTokenSource = new ();
			subjectTokenSource.Setup( x => x.GetSubjectToken() ).Returns( "subject-abc" );

			TokenExchangeAccessTokenManager manager = new ( stubServer.Issuer, "client-id", "client-secret", "scope-a",
															subjectTokenSource.Object, stubServer.CreateClientFactory() );

			manager.GetAccessToken();

			RecordedRequest tokenRequest = Assert.Single( stubServer.TokenRequests );
			IDictionary<string, string> fields = tokenRequest.FormFields();

			Assert.Equal( TokenExchangeAccessTokenManager.GrantType, fields["grant_type"] );
			Assert.Equal( "client-id", fields["client_id"] );
			Assert.Equal( "client-secret", fields["client_secret"] );
			Assert.Equal( "scope-a", fields["scope"] );
			Assert.Equal( "subject-abc", fields["subject_token"] );
			Assert.Equal( TokenExchangeAccessTokenManager.SubjectTokenType, fields["subject_token_type"] );

			subjectTokenSource.Verify( x => x.GetSubjectToken(), Times.AtLeastOnce() );
		}

		[Fact]
		public async Task GetNewAccessTokenAsync_SendsTokenExchangeGrantWithSubjectTokenFromSource()
		{
			// Exercises the Uri-endpoint ctor overload.
			OAuth2StubServer stubServer = new ();
			Mock<ITokenExchangeSubjectTokenSource> subjectTokenSource = new ();
			subjectTokenSource.Setup( x => x.GetSubjectToken() ).Returns( "subject-xyz" );

			TokenExchangeAccessTokenManager manager = new ( new Uri( stubServer.Issuer ), "client-id", "client-secret", "scope-a",
															subjectTokenSource.Object, stubServer.CreateClientFactory() );

			await manager.GetAccessTokenAsync();

			RecordedRequest tokenRequest = Assert.Single( stubServer.TokenRequests );
			IDictionary<string, string> fields = tokenRequest.FormFields();

			Assert.Equal( TokenExchangeAccessTokenManager.GrantType, fields["grant_type"] );
			Assert.Equal( "client-id", fields["client_id"] );
			Assert.Equal( "client-secret", fields["client_secret"] );
			Assert.Equal( "scope-a", fields["scope"] );
			Assert.Equal( "subject-xyz", fields["subject_token"] );
			Assert.Equal( TokenExchangeAccessTokenManager.SubjectTokenType, fields["subject_token_type"] );

			subjectTokenSource.Verify( x => x.GetSubjectToken(), Times.AtLeastOnce() );
		}

		private static TokenExchangeAccessTokenManager CreateManager( OAuth2StubServer stubServer ) =>
			new ( new Uri( stubServer.Issuer ), "client-id", "secret", "scope",
					new StaticTokenExchangeSubjectTokenSource( "subject-token" ), stubServer.CreateClientFactory() );

		[Fact]
		public void GetAccessToken_ProviderReturns400_ThrowsAuthenticationExceptionWithProviderError()
		{
			OAuth2StubServer stubServer = new ();
			stubServer.AlwaysRespondWith( HttpStatusCode.BadRequest, OAuth2StubServer.ErrorJson( "invalid_target", "unknown resource" ) );
			TokenExchangeAccessTokenManager manager = CreateManager( stubServer );

			AuthenticationException exception = Assert.Throws<AuthenticationException>( () => manager.GetAccessToken() );

			Assert.Equal( "invalid_target: unknown resource", exception.Message );
		}

		[Fact]
		public void GetAccessToken_ProviderReturns401_ThrowsAuthenticationExceptionWithProviderError()
		{
			OAuth2StubServer stubServer = new ();
			stubServer.AlwaysRespondWith( HttpStatusCode.Unauthorized, OAuth2StubServer.ErrorJson( "invalid_target", "unknown resource" ) );
			TokenExchangeAccessTokenManager manager = CreateManager( stubServer );

			AuthenticationException exception = Assert.Throws<AuthenticationException>( () => manager.GetAccessToken() );

			Assert.Equal( "invalid_target: unknown resource", exception.Message );
		}

		[Fact]
		public void GetAccessToken_ProviderReturns500_ThrowsHttpApiRequestException()
		{
			OAuth2StubServer stubServer = new ();
			stubServer.AlwaysRespondWith( HttpStatusCode.InternalServerError, "" );
			TokenExchangeAccessTokenManager manager = CreateManager( stubServer );

			HttpApiRequestException exception = Assert.Throws<HttpApiRequestException>( () => manager.GetAccessToken() );

			Assert.Equal( "Unexpected response status code received from OAuth2 provider: 500", exception.Message );
		}

		[Fact]
		public void GetAccessToken_ProviderReturnsUnexpected2xx_ThrowsHttpApiRequestException()
		{
			OAuth2StubServer stubServer = new ();
			stubServer.AlwaysRespondWith( HttpStatusCode.NoContent, "" );
			TokenExchangeAccessTokenManager manager = CreateManager( stubServer );

			HttpApiRequestException exception = Assert.Throws<HttpApiRequestException>( () => manager.GetAccessToken() );

			Assert.Equal( "Unexpected response status code received from OAuth2 provider: 204", exception.Message );
		}
	}
}
