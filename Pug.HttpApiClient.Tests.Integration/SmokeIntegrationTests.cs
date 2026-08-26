using System;
using System.Threading.Tasks;
using Pug.HttpApiClient.OAuth2;
using Xunit;

namespace Pug.HttpApiClient.Tests.Integration
{
	[Collection( KeycloakCollection.Name )]
	[Trait( "Category", "Integration" )]
	public class SmokeIntegrationTests
	{
		private readonly KeycloakFixture _keycloak;

		public SmokeIntegrationTests( KeycloakFixture keycloak )
		{
			_keycloak = keycloak;
		}

		[SkippableFact]
		public async Task ClientCredentials_AgainstRealKeycloak_ReturnsUsableAccessToken()
		{
			Skip.If( _keycloak.SkipReason is not null, _keycloak.SkipReason );

			ClientAccessTokenManager manager = new (
					new Uri( _keycloak.Issuer ), "api-client", "api-secret", "openid",
					_keycloak.HttpClientFactory );

			AccessToken token = await manager.GetAccessTokenAsync();

			Assert.False( string.IsNullOrWhiteSpace( token.Token ) );
			Assert.Equal( "Bearer", token.TokenType );
			Assert.True( token.ValidityPeriod > 0 );
			// a Keycloak access token is a JWT: three dot-separated segments
			Assert.Equal( 3, token.Token.Split( '.' ).Length );
		}
	}
}
