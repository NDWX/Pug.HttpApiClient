using System.Runtime.Serialization;

namespace Pug.HttpApiClient.OAuth2Decorators
{
	public record AccessToken
	{
		[DataMember(Name = "access_token")]
		public string Token { get; set; }

		[DataMember(Name = "token_type")]
		public string TokenType { get; set; }

		[DataMember(Name = "expires_in")]
		public int ValidityPeriod { get; set; }
	}
}