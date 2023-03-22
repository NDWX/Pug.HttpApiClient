using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Pug.HttpApiClient
{
	public interface IHttpApiClient
	{
		Uri BaseAddress { get; }
		string BasePath { get; }
		Uri BaseUrl { get; }

		Task<HttpResponseMessage> GetAsync( string path,
											MediaTypeWithQualityHeaderValue mediaType,
											IDictionary<string, string> headers,
											IDictionary<string, string> queries );

		Task<HttpResponseMessage> PostAsync( string path, HttpContent content,
											MediaTypeWithQualityHeaderValue mediaType, IDictionary<string, string> headers = null,
											IDictionary<string, string> queries = null );

		Task<HttpResponseMessage> PutAsync( string path, HttpContent content,
											MediaTypeWithQualityHeaderValue mediaType, IDictionary<string, string> headers,
											IDictionary<string, string> queries );

		Task<HttpResponseMessage> DeleteAsync( string path,
												IDictionary<string, string> headers = null,
												IDictionary<string, string> queries = null,
												MediaTypeWithQualityHeaderValue mediaType = null );
#if !NETSTANDARD
		Task<HttpResponseMessage> PatchAsync( string path,
											HttpContent content,
											MediaTypeWithQualityHeaderValue mediaType, IDictionary<string, string> headers,
											IDictionary<string, string> queries );
#endif
	}
}