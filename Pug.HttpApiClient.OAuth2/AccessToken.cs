using System.Runtime.Serialization;
using Newtonsoft.Json;
#if !(NETCOREAPP2_1 || NETSTANDARD)
using System.Text.Json.Serialization;
#endif

namespace Pug.HttpApiClient.OAuth2
{
	public record AccessToken
	{
		[DataMember(Name = "access_token")]
		[JsonProperty( "access_token")]
#if !(NETCOREAPP2_1 || NETSTANDARD)
		[JsonPropertyName( "access_token")]
#endif
		public string Token { get; set; }

		[DataMember(Name = "token_type")]
		[JsonProperty( "token_type")]
#if !(NETCOREAPP2_1 || NETSTANDARD)
		[JsonPropertyName( "token_type")]
#endif
		public string TokenType { get; set; }

		[DataMember(Name = "expires_in")]
		[JsonProperty( "expires_in")]
#if !(NETCOREAPP2_1 || NETSTANDARD)
		[JsonPropertyName("expires_in")]
#endif
		public int ValidityPeriod { get; set; }
	}
}