using System.Runtime.Serialization;
using Newtonsoft.Json;
#if !(NETCOREAPP2_1 || NETSTANDARD)
using System.Text.Json.Serialization;
#endif

namespace Pug.HttpApiClient.OAuth2
{
	public record RefreshableAccessToken : AccessToken
	{
		[DataMember(Name = "refresh_token")]
		[JsonProperty( "refresh_token")]
#if !(NETCOREAPP2_1 || NETSTANDARD)
		[JsonPropertyName("refresh_token")]
#endif
		public string RefreshToken { get; set; }
	}
}