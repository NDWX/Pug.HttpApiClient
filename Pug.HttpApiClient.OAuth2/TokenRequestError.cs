using System.Runtime.Serialization;

namespace Pug.HttpApiClient.OAuth2Decorators
{
	[DataContract]
	public record TokenRequestError
	{
		[DataMember(Name = "error")]
		public string Message { get; set; }
	}
}