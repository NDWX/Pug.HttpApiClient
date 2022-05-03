using System.Runtime.Serialization;

namespace Pug.HttpApiClient.OAuth2Decorators
{
	[DataContract]
	public record ClientAccessToken : AccessToken
	{
		[DataMember(Name = "scope")]
		public string Scope { get; set; }
	}
}