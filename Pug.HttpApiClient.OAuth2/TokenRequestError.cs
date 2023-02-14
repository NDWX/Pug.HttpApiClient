using System.Runtime.Serialization;

namespace Pug.HttpApiClient.OAuth2
{
	[DataContract]
	public record TokenRequestError
	{
		[DataMember(Name = "error")]
		public string Message { get; set; }
	}
}