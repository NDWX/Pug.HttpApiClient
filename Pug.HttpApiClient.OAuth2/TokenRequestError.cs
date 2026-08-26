using System.Runtime.Serialization;
#if !(NETCOREAPP2_1 || NETSTANDARD)
using System.Text.Json.Serialization;
#endif

namespace Pug.HttpApiClient.OAuth2
{
	/// <summary>
	/// Error response returned by an OAuth2 token endpoint, per RFC 6749 section 5.2.
	/// </summary>
	[DataContract]
	public record TokenRequestError
	{
		/// <summary>
		/// Single ASCII error code, e.g. <c>invalid_client</c> or <c>invalid_grant</c>.
		/// </summary>
		[DataMember(Name = "error")]
#if !(NETCOREAPP2_1 || NETSTANDARD)
		[JsonPropertyName( "error" )]
#endif
		public string Message { get; set; }

		/// <summary>
		/// Human-readable elaboration of <see cref="Message"/>. Optional per the specification.
		/// </summary>
		[DataMember(Name = "error_description")]
#if !(NETCOREAPP2_1 || NETSTANDARD)
		[JsonPropertyName( "error_description" )]
#endif
		public string Description { get; set; }

		/// <summary>
		/// <see cref="Message"/> combined with <see cref="Description"/> when one was supplied.
		/// </summary>
		public string FullMessage =>
			string.IsNullOrWhiteSpace( Description ) ? Message : $"{Message}: {Description}";
	}
}
