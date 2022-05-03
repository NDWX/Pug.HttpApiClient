using System.Collections.Generic;
using System.Net.Http.Headers;

namespace Pug.HttpApiClient
{
	public class MessageDecorationContext : DecorationContext
	{
		public MessageDecorationContext( HttpRequestHeaders requestHeaders ) : base(requestHeaders)
		{
		}

		public IDictionary<string, string> UrlQueries { get; set; }
	}
}