using System;
using System.Threading.Tasks;
using Pug.HttpApiClient.Json;
using Pug.HttpApiClient.OAuth2;
using Xunit;

namespace Pug.HttpApiClient.Tests.Integration
{
	[Collection( KeycloakCollection.Name )]
	[Trait( "Category", "Integration" )]
	public class KeycloakDiscoveryTests
	{
		private readonly KeycloakFixture _keycloak;

		public KeycloakDiscoveryTests( KeycloakFixture keycloak )
		{
			_keycloak = keycloak;
		}

		[SkippableFact]
		public async Task GetOpenIdConfigurationAsync_PathBearingIssuer_ReturnsRealProviderEndpoints()
		{
			Skip.If( _keycloak.SkipReason is not null, _keycloak.SkipReason );

			// Same construction AccessTokenManager.GetOpenIdConfiguration[Async] performs internally -
			// Fix A lives in the HttpApiClient constructor, so this exercises it directly against a
			// path-bearing issuer (".../realms/acme"), which is exactly what the old BaseAddress logic
			// mangled ("https://auth.example.com/auth" became "https:/.example.com").
			IHttpApiClient apiClient = new HttpApiClient( new Uri( _keycloak.Issuer ), _keycloak.HttpClientFactory );

			OpenIdConfiguration configuration = await apiClient.GetAsync<OpenIdConfiguration>( "/.well-known/openid-configuration" );

			Assert.StartsWith( _keycloak.Issuer, configuration.TokenEndpoint );
			Assert.StartsWith( _keycloak.Issuer, configuration.AuthorizationEndpoint );
			Assert.False( string.IsNullOrWhiteSpace( configuration.JwksUri ) );
			Assert.StartsWith( _keycloak.Issuer, configuration.JwksUri );
			Assert.Contains( "openid", configuration.SupportedScope );
		}
	}
}
