using System.Runtime.Serialization;
using Newtonsoft.Json;
#if !NETCOREAPP2_1
using System.Text.Json.Serialization;
#endif

namespace Pug.HttpApiClient.OAuth2Decorators
{
	[DataContract]
	public record ClientAccessToken : AccessToken
	{
		[DataMember(Name = "scope")]
		[JsonProperty( "scope")]
#if !NETCOREAPP2_1
		[JsonPropertyName("scope")]
#endif
		public string Scope { get; set; }
	}
}