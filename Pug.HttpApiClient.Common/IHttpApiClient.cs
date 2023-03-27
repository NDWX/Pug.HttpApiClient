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

		/// <summary>
		/// Invoke HTTP GET call
		/// </summary>
		/// <param name="path">Request path</param>
		/// <param name="mediaType">HTTP media type</param>
		/// <param name="headers">HTTP headers</param>
		/// <param name="queries">URL Queries</param>
		/// <returns>Instance of HttpResponseMessage</returns>
		Task<HttpResponseMessage> GetAsync( string path,
											MediaTypeWithQualityHeaderValue mediaType,
											IDictionary<string, string> headers,
											IDictionary<string, string> queries );

		/// <summary>
		/// Invoke HTTP POST call
		/// </summary>
		/// <param name="path">Request path</param>
		/// <param name="content">HTTP content</param>
		/// <param name="mediaType">HTTP media type</param>
		/// <param name="headers">HTTP headers</param>
		/// <param name="queries">URL Queries</param>
		/// <returns>Instance of HttpResponseMessage</returns>
		Task<HttpResponseMessage> PostAsync( string path, HttpContent content,
											MediaTypeWithQualityHeaderValue mediaType, IDictionary<string, string> headers = null,
											IDictionary<string, string> queries = null );

		/// <summary>
		/// Invoke HTTP PUT call
		/// </summary>
		/// <param name="path">Request path</param>
		/// <param name="content">HTTP content</param>
		/// <param name="mediaType">HTTP media type</param>
		/// <param name="headers">HTTP headers</param>
		/// <param name="queries">URL Queries</param>
		/// <returns>Instance of HttpResponseMessage</returns>
		Task<HttpResponseMessage> PutAsync( string path, HttpContent content,
											MediaTypeWithQualityHeaderValue mediaType, IDictionary<string, string> headers,
											IDictionary<string, string> queries );

		/// <summary>
		/// Invoke HTTP DELETE call
		/// </summary>
		/// <param name="path">Request path</param>
		/// <param name="mediaType">HTTP media type</param>
		/// <param name="headers">HTTP headers</param>
		/// <param name="queries">URL Queries</param>
		/// <returns>Instance of HttpResponseMessage</returns>
		Task<HttpResponseMessage> DeleteAsync( string path,
												IDictionary<string, string> headers = null,
												IDictionary<string, string> queries = null,
												MediaTypeWithQualityHeaderValue mediaType = null );

		/// <summary>
		/// Invoke HTTP PATCH call
		/// </summary>
		/// <param name="path">Request path</param>
		/// <param name="content">HTTP content</param>
		/// <param name="mediaType">HTTP media type</param>
		/// <param name="headers">HTTP headers</param>
		/// <param name="queries">URL Queries</param>
		/// <returns>Instance of HttpResponseMessage</returns>
		Task<HttpResponseMessage> PatchAsync( string path,
											HttpContent content,
											MediaTypeWithQualityHeaderValue mediaType, IDictionary<string, string> headers,
											IDictionary<string, string> queries );
	}
}