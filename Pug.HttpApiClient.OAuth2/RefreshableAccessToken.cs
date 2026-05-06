using System.Runtime.Serialization;
#if !(NETCOREAPP2_1 || NETSTANDARD)
using System.Text.Json.Serialization;
#else
using Newtonsoft.Json;
#endif

namespace Pug.HttpApiClient.OAuth2
{
	public record RefreshableAccessToken : AccessToken
	{
		[DataMember(Name = "refresh_token")]
#if !(NETCOREAPP2_1 || NETSTANDARD)
		[JsonPropertyName("refresh_token")]
		#else
		[JsonProperty( "refresh_token")]
#endif
		public string RefreshToken { get; set; }
	}
}