using System.Runtime.Serialization;
#if !NETCOREAPP2_1
using System.Text.Json.Serialization;
#endif

namespace Pug.HttpApiClient.OAuth2Decorators
{
	public record AccessToken
	{
		[DataMember(Name = "access_token")]
#if !NETCOREAPP2_1
		[JsonPropertyName( "token_type")]
#endif
		public string Token { get; set; }

		[DataMember(Name = "token_type")]
#if !NETCOREAPP2_1
		[JsonPropertyName( "token_type")]
#endif
		public string TokenType { get; set; }

		[DataMember(Name = "expires_in")]
#if !NETCOREAPP2_1
		[JsonPropertyName("token_type")]
#endif
		public int ValidityPeriod { get; set; }
	}
}