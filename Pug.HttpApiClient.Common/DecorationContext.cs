using System.Net.Http.Headers;

namespace Pug.HttpApiClient
{
	public class DecorationContext
	{
		private readonly HttpRequestHeaders _requestHeaders;

		public DecorationContext( HttpRequestHeaders requestHeaders )
		{
			_requestHeaders = requestHeaders;
		}

		public HttpRequestHeaders RequestHeaders => _requestHeaders;
	}
}