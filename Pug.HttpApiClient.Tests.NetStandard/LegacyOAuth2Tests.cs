using System;
using System.Net;
using System.Security.Authentication;
using System.Threading.Tasks;
using Pug.HttpApiClient.OAuth2;
using Pug.HttpApiClient.Tests.Infrastructure;
using Xunit;

namespace Pug.HttpApiClient.Tests.NetStandard
{
	/// <summary>
	/// <see cref="TokenRequestError"/> carries <c>[DataContract]</c>/<c>[DataMember(Name="error")]</c>, and only
	/// gained an explicit <c>[JsonPropertyName("error")]</c> for the System.Text.Json (non-netstandard) build.
	/// Newtonsoft.Json has built-in support for DataContract/DataMember attributes (opt-in serialization keyed by
	/// DataMember.Name), so on this TFM the mapping worked correctly with no JsonPropertyName at all - which is
	/// exactly why the missing-attribute defect on the modern build stayed invisible here.
	/// </summary>
	[Trait( "Category", "NetStandard" )]
	public class LegacyOAuth2Tests
	{
		[Fact]
		public async Task GetAccessTokenAsync_400Response_MapsProviderErrorViaDataMemberOnNewtonsoft()
		{
			OAuth2StubServer stubServer = new ();
			stubServer.EnqueueTokenResponse( HttpStatusCode.BadRequest,
					OAuth2StubServer.ErrorJson( "invalid_client", "Client authentication failed" )
				);

			ClientAccessTokenManager manager = new (
					new Uri( stubServer.Issuer ), "client-id", "client-secret", "scope-a", stubServer.CreateClientFactory()
				);

			AuthenticationException exception =
				await Assert.ThrowsAsync<AuthenticationException>( () => manager.GetAccessTokenAsync() );

			Assert.Equal( "invalid_client: Client authentication failed", exception.Message );
		}

		[Fact]
		public async Task GetAccessTokenAsync_ClientCredentialsHappyPath_DeserializesTokenThroughNewtonsoft()
		{
			OAuth2StubServer stubServer = new (); // default AlwaysRespondWith == 200 AccessTokenJson()

			ClientAccessTokenManager manager = new (
					new Uri( stubServer.Issuer ), "client-id", "client-secret", "scope-a", stubServer.CreateClientFactory()
				);

			AccessToken token = await manager.GetAccessTokenAsync();

			Assert.Equal( "access-token-value", token.Token );
			Assert.Equal( "Bearer", token.TokenType );
			Assert.Equal( 3600, token.ValidityPeriod );
		}
	}
}
