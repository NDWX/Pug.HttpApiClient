using System.Runtime.Serialization;
#if !(NETCOREAPP2_1 || NETSTANDARD)
using System.Text.Json.Serialization;
#else
using Newtonsoft.Json;
#endif

namespace Pug.HttpApiClient.OAuth2
{
	public record AccessToken
	{
		[DataMember( Name = "access_token" )]
#if !(NETCOREAPP2_1 || NETSTANDARD)
		[JsonPropertyName( "access_token" )]
#else
		[JsonProperty( "access_token")]
#endif
		public string Token { get; set; }

		[DataMember( Name = "token_type" )]
#if !(NETCOREAPP2_1 || NETSTANDARD)
		[JsonPropertyName( "token_type" )]
#else
		[JsonProperty( "token_type")]
#endif
		public string TokenType { get; set; }

		/// <remarks>
		/// DIVERGENCE - typed as <see cref="int"/> and deserialized with default options, so a provider that
		/// emits <c>expires_in</c> as a JSON STRING (<c>"3600"</c> rather than <c>3600</c>) fails with a
		/// JsonException instead of binding. RFC 6749 section 5.1 specifies a number, and Keycloak - the
		/// provider exercised by the integration suite - emits a number, so this is correct against the
		/// specification; it is noted because some providers in the wild are lax here. Fixing it would mean
		/// deserializing tokens with JsonNumberHandling.AllowReadingFromString.
		/// </remarks>
		[DataMember( Name = "expires_in" )]
#if !(NETCOREAPP2_1 || NETSTANDARD)
		[JsonPropertyName( "expires_in" )]
#else
		[JsonProperty( "expires_in")]
#endif
		public int ValidityPeriod { get; set; }
	}
}