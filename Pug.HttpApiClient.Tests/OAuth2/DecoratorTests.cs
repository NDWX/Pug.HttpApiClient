using System;
using System.Net.Http;
using System.Threading.Tasks;
using Moq;
using Pug.HttpApiClient.OAuth2;
using Pug.HttpApiClient.Tests.Infrastructure;
using Xunit;

namespace Pug.HttpApiClient.Tests.OAuth2
{
	public class DecoratorTests
	{
		[Fact]
		public void AccessTokenMessageDecorator_Constructor_NullProvider_ThrowsArgumentNullException()
		{
			ArgumentNullException exception = Assert.Throws<ArgumentNullException>( () => new AccessTokenMessageDecorator( null ) );

			Assert.Equal( "accessTokenManager", exception.ParamName );
		}

		[Fact]
		public void Decorate_SetsAuthorizationHeaderFromProvider()
		{
			Mock<IAccessTokenProvider<AccessToken>> provider = new ();
			provider.Setup( x => x.GetAccessToken() )
					.Returns( new AccessToken { Token = "tok", TokenType = "Bearer", ValidityPeriod = 3600 } );

			AccessTokenMessageDecorator decorator = new ( provider.Object );
			HttpRequestMessage request = new ( HttpMethod.Get, "https://api.example.com" );
			MessageDecorationContext context = new ( request.Headers );

			decorator.Decorate( context );

			Assert.Equal( "Bearer", request.Headers.Authorization.Scheme );
			Assert.Equal( "tok", request.Headers.Authorization.Parameter );
		}

		[Fact]
		public async Task DecorateAsync_SetsAuthorizationHeaderFromProvider()
		{
			Mock<IAccessTokenProvider<AccessToken>> provider = new ();
			provider.Setup( x => x.GetAccessTokenAsync() )
					.ReturnsAsync( new AccessToken { Token = "tok-async", TokenType = "Bearer", ValidityPeriod = 3600 } );

			AccessTokenMessageDecorator decorator = new ( provider.Object );
			HttpRequestMessage request = new ( HttpMethod.Get, "https://api.example.com" );
			MessageDecorationContext context = new ( request.Headers );

			await decorator.DecorateAsync( context );

			Assert.Equal( "Bearer", request.Headers.Authorization.Scheme );
			Assert.Equal( "tok-async", request.Headers.Authorization.Parameter );
		}

		[Fact]
		public void ClientCredentialsDecorator_ManagerCtor_Constructs()
		{
			ClientAccessTokenManager manager = new ( new Uri( "https://auth.example.com" ), "client", "secret", "scope",
														Mock.Of<IHttpClientFactory>() );

			ClientCredentialsDecorator decorator = new ( manager );

			Assert.NotNull( decorator );
		}

		[Fact]
		public void ClientCredentialsDecorator_UriCtor_Constructs()
		{
			ClientCredentialsDecorator decorator = new ( new Uri( "https://auth.example.com" ), "client", "secret", "scope",
															Mock.Of<IHttpClientFactory>() );

			Assert.NotNull( decorator );
		}

		[Fact]
		public void ClientCredentialsDecorator_StringCtor_Constructs()
		{
			ClientCredentialsDecorator decorator = new ( "https://auth.example.com", "client", "secret", "scope",
															Mock.Of<IHttpClientFactory>() );

			Assert.NotNull( decorator );
		}

		[Fact]
		public void TokenExchangeCredentialsDecorator_ManagerCtor_Constructs()
		{
			TokenExchangeAccessTokenManager manager = new ( new Uri( "https://auth.example.com" ), "client", "secret", "scope",
																Mock.Of<ITokenExchangeSubjectTokenSource>(), Mock.Of<IHttpClientFactory>() );

			TokenExchangeCredentialsDecorator decorator = new ( manager );

			Assert.NotNull( decorator );
		}

		[Fact]
		public void TokenExchangeCredentialsDecorator_UriAndSourceCtor_Constructs()
		{
			TokenExchangeCredentialsDecorator decorator = new ( new Uri( "https://auth.example.com" ), "client", "secret", "scope",
																Mock.Of<ITokenExchangeSubjectTokenSource>(), Mock.Of<IHttpClientFactory>() );

			Assert.NotNull( decorator );
		}

		[Fact]
		public void TokenExchangeCredentialsDecorator_StringAndSourceCtor_Constructs()
		{
			TokenExchangeCredentialsDecorator decorator = new ( "https://auth.example.com", "client", "secret", "scope",
																Mock.Of<ITokenExchangeSubjectTokenSource>(), Mock.Of<IHttpClientFactory>() );

			Assert.NotNull( decorator );
		}

		[Fact]
		public void TokenExchangeCredentialsDecorator_StaticSubjectTokenCtor_Constructs()
		{
			TokenExchangeCredentialsDecorator decorator = new ( "https://auth.example.com", "client", "secret", "scope",
																"static-subject-token", Mock.Of<IHttpClientFactory>() );

			Assert.NotNull( decorator );
		}

		[Fact]
		public async Task HttpApiClient_WithAccessTokenMessageDecorator_SendsBearerAuthorizationHeader()
		{
			Mock<IAccessTokenProvider<AccessToken>> provider = new ();
			provider.Setup( x => x.GetAccessTokenAsync() )
					.ReturnsAsync( new AccessToken { Token = "wired-token", TokenType = "Bearer", ValidityPeriod = 3600 } );

			AccessTokenMessageDecorator decorator = new ( provider.Object );

			StubHttpMessageHandler handler = new ( _ => Responses.Json( "{}" ) );
			StubHttpClientFactory factory = new ( handler );

			Pug.HttpApiClient.HttpApiClient client = new ( new Uri( "https://api.example.com/v1" ), factory, null,
															new IHttpRequestMessageDecorator[] { decorator } );

			await client.GetAsync( "/resource", null, null, null );

			RecordedRequest request = Assert.Single( handler.Requests );

			Assert.Equal( "Bearer", request.AuthorizationScheme );
			Assert.Equal( "wired-token", request.AuthorizationParameter );
		}
	}
}
