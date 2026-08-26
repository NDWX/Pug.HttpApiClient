using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace Pug.HttpApiClient.Tests.Integration
{
	/// <summary>
	/// A real Keycloak, started once per test class collection, seeded from acme-realm.json.
	/// </summary>
	public sealed class KeycloakFixture : IAsyncLifetime
	{
		private const string Image = "quay.io/keycloak/keycloak:26.0";
		private const int KeycloakPort = 8080;

		private IContainer _container;

		/// <summary>Why the container is unavailable, or <c>null</c> when it started.</summary>
		public string SkipReason { get; private set; }

		/// <summary>Issuer URL of the seeded realm - a path-bearing URL, as real providers use.</summary>
		public string Issuer => $"{BaseUrl}/realms/acme";

		public string BaseUrl => $"http://{_container.Hostname}:{_container.GetMappedPublicPort( KeycloakPort )}";

		public IHttpClientFactory HttpClientFactory { get; } = new SimpleHttpClientFactory();

		public async Task InitializeAsync()
		{
			string realmJson = Path.Combine( AppContext.BaseDirectory, "acme-realm.json" );

			try
			{
				_container = new ContainerBuilder()
							.WithImage( Image )
							.WithEnvironment( "KC_BOOTSTRAP_ADMIN_USERNAME", "admin" )
							.WithEnvironment( "KC_BOOTSTRAP_ADMIN_PASSWORD", "admin" )
							.WithEnvironment( "KC_HEALTH_ENABLED", "true" )
							.WithResourceMapping( new FileInfo( realmJson ), "/opt/keycloak/data/import/" )
							.WithCommand( "start-dev", "--import-realm" )
							.WithPortBinding( KeycloakPort, true )
							.WithWaitStrategy(
									Wait.ForUnixContainer()
										.UntilHttpRequestIsSucceeded(
											request => request.ForPath( "/realms/acme/.well-known/openid-configuration" )
															.ForPort( KeycloakPort ) ) )
							.Build();

				await _container.StartAsync().ConfigureAwait( false );
			}
			catch( Exception exception )
			{
				// No reachable Docker daemon, no image, or startup failure: report it as a skip reason so the
				// suite degrades to "not run" rather than to a wall of failures on a machine without Docker.
				SkipReason = $"Keycloak container unavailable: {exception.GetType().Name}: {exception.Message}";
			}
		}

		public async Task DisposeAsync()
		{
			if( _container is not null )
				await _container.DisposeAsync().ConfigureAwait( false );
		}

		private sealed class SimpleHttpClientFactory : IHttpClientFactory
		{
			public HttpClient CreateClient( string name ) => new ();
		}
	}

	[CollectionDefinition( KeycloakCollection.Name )]
	public sealed class KeycloakCollection : ICollectionFixture<KeycloakFixture>
	{
		public const string Name = "keycloak";
	}
}
