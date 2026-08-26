using System.Net;
using System.Net.Http;
using System.Text;

namespace Pug.HttpApiClient.Tests.Infrastructure
{
	public static class Responses
	{
		public static HttpResponseMessage Create( HttpStatusCode statusCode, string body = "",
													string contentType = "application/json",
													string reasonPhrase = null )
		{
			HttpResponseMessage response = new ( statusCode )
			{
				Content = new StringContent( body ?? string.Empty, Encoding.UTF8, contentType )
			};

			if( reasonPhrase is not null )
				response.ReasonPhrase = reasonPhrase;

			return response;
		}

		public static HttpResponseMessage Json( string body ) => Create( HttpStatusCode.OK, body );

		public static HttpResponseMessage Status( HttpStatusCode statusCode, string reasonPhrase = null ) =>
			Create( statusCode, string.Empty, "text/plain", reasonPhrase );
	}
}
