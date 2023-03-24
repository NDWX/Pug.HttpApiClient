using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Threading.Tasks;
#if NETCOREAPP2_1 || NETSTANDARD
using Newtonsoft.Json;
#else
using System.Text.Json;
#endif

namespace Pug.HttpApiClient.Json
{

	public static class IHttpApiClientJsonExtensions
	{
		private const string MediaTypeName = "application/json";

		/// <summary>
		/// Invoke GET call with JSON request and response type.
		/// </summary>
		/// <param name="httpApiClient">Instance of IHttpApiClient</param>
		/// <param name="path">Request path</param>
		/// <param name="headers">HTTP headers</param>
		/// <param name="queries">URL Queries</param>
		/// <typeparam name="T">Serializable class instance</typeparam>
		/// <returns>Instance of <typeparamref name="T"/></returns>
		/// <exception cref="Pug.Application.Security.NotAuthorized">When server returned 403 (Forbidden)</exception>
		/// <exception cref="Pug.HttpApiClient.UnknownResourceException">When server returned 404 (Not Found) or 410 (Gone)</exception>
		/// <exception cref="System.Security.Authentication.AuthenticationException">When server returned 401 (Unauthorized)</exception>
		/// <exception cref="Pug.HttpApiClient.HttpApiRequestException">When server returned 40x. InvalidOperationException inner exception will be specified when server returned 405 (Method Not Allowed) or 423 (Locked).</exception>
		/// <exception cref="Pug.HttpApiClient.InternalServerErrorException">When server returned 500 (Internal Server Error) or 507 (Insufficient Storage)</exception>
		public static async Task<T> GetAsync<T>(this IHttpApiClient httpApiClient, string path, IDictionary<string, string> headers = null, IDictionary<string, string> queries = null )
		{
			HttpResponseMessage response = await httpApiClient.GetAsync(
													path, new MediaTypeWithQualityHeaderValue( MediaTypeName ),
													headers, queries
												);

			if( !response.IsSuccessStatusCode ) throw new HttpApiRequestException( response );
			
			string openIdConfigurationString = await response.Content.ReadAsStringAsync();
			
#if NETCOREAPP2_1 || NETSTANDARD
			return JsonConvert.DeserializeObject<T>( openIdConfigurationString );
#else
			return JsonSerializer.Deserialize<T>( openIdConfigurationString );
#endif

		}


		/// <summary>
		/// Invoke POST call with JSON request and response type.
		/// </summary>
		/// <param name="httpApiClient">Instance of IHttpApiClient</param>
		/// <param name="path">Request path</param>
		/// <param name="content">Request content</param>
		/// <param name="headers">HTTP headers</param>
		/// <param name="queries">URL Queries</param>
		/// <typeparam name="TContent">Serializable class instance</typeparam>
		/// <returns>Instance of HttpResponseMessage</returns>
		/// <exception cref="Pug.Application.Security.NotAuthorized">When server returned 403 (Forbidden)</exception>
		/// <exception cref="Pug.HttpApiClient.UnknownResourceException">When server returned 404 (Not Found) or 410 (Gone)</exception>
		/// <exception cref="System.Security.Authentication.AuthenticationException">When server returned 401 (Unauthorized)</exception>
		/// <exception cref="Pug.HttpApiClient.HttpApiRequestException">When server returned 40x. InvalidOperationException inner exception will be specified when server returned 405 (Method Not Allowed) or 423 (Locked).</exception>
		/// <exception cref="Pug.HttpApiClient.InternalServerErrorException">When server returned 500 (Internal Server Error) or 507 (Insufficient Storage)</exception>
		public static Task<HttpResponseMessage> PostAsync<TContent>( this IHttpApiClient httpApiClient, string path, TContent content, IDictionary<string, string> headers = null,
																	IDictionary<string, string> queries = null )
		{
			HttpContent httpContent;
			string contentJson;

#if NETCOREAPP2_1 || NETSTANDARD
			contentJson = JsonConvert.SerializeObject( content );
#else
			contentJson = JsonSerializer.Serialize( content );
#endif

			httpContent = new StringContent( contentJson );

			return httpApiClient.PostAsync( path, httpContent, new MediaTypeWithQualityHeaderValue( MediaTypeName ), headers, queries );
		}

		/// <summary>
		/// Invoke PUT call with JSON request and response type.
		/// </summary>
		/// <param name="httpApiClient">Instance of IHttpApiClient</param>
		/// <param name="path">Request path</param>
		/// <param name="content">Request content</param>
		/// <param name="headers">HTTP headers</param>
		/// <param name="queries">URL Queries</param>
		/// <typeparam name="TContent">Serializable class instance</typeparam>
		/// <returns>Instance of HttpResponseMessage</returns>
		/// <exception cref="Pug.Application.Security.NotAuthorized">When server returned 403 (Forbidden)</exception>
		/// <exception cref="Pug.HttpApiClient.UnknownResourceException">When server returned 404 (Not Found) or 410 (Gone)</exception>
		/// <exception cref="System.Security.Authentication.AuthenticationException">When server returned 401 (Unauthorized)</exception>
		/// <exception cref="Pug.HttpApiClient.HttpApiRequestException">When server returned 40x. InvalidOperationException inner exception will be specified when server returned 405 (Method Not Allowed) or 423 (Locked).</exception>
		/// <exception cref="Pug.HttpApiClient.InternalServerErrorException">When server returned 500 (Internal Server Error) or 507 (Insufficient Storage)</exception>
		public static Task<HttpResponseMessage> PutAsync<TContent>( this IHttpApiClient httpApiClient, string path, TContent content, IDictionary<string, string> headers = null,
																	IDictionary<string, string> queries = null )
		{
			HttpContent httpContent;
			string contentJson;

#if NETCOREAPP2_1 || NETSTANDARD
			contentJson = JsonConvert.SerializeObject( content );
#else
			contentJson = JsonSerializer.Serialize( content );
#endif

			httpContent = new StringContent( contentJson );

			return httpApiClient.PutAsync( path, httpContent, new MediaTypeWithQualityHeaderValue( MediaTypeName ), headers, queries );
		}

#if !NETSTANDARD

		/// <summary>
		/// Invoke PATCH call with JSON request and response type.
		/// </summary>
		/// <param name="httpApiClient">Instance of IHttpApiClient</param>
		/// <param name="path">Request path</param>
		/// <param name="content">Request content</param>
		/// <param name="headers">HTTP headers</param>
		/// <param name="queries">URL Queries</param>
		/// <typeparam name="TContent">Serializable class instance</typeparam>
		/// <returns>Instance of HttpResponseMessage</returns>
		/// <exception cref="Pug.Application.Security.NotAuthorized">When server returned 403 (Forbidden)</exception>
		/// <exception cref="Pug.HttpApiClient.UnknownResourceException">When server returned 404 (Not Found) or 410 (Gone)</exception>
		/// <exception cref="System.Security.Authentication.AuthenticationException">When server returned 401 (Unauthorized)</exception>
		/// <exception cref="Pug.HttpApiClient.HttpApiRequestException">When server returned 40x. InvalidOperationException inner exception will be specified when server returned 405 (Method Not Allowed) or 423 (Locked).</exception>
		/// <exception cref="Pug.HttpApiClient.InternalServerErrorException">When server returned 500 (Internal Server Error) or 507 (Insufficient Storage)</exception>
		public static Task<HttpResponseMessage> PatchAsync<TContent>( this IHttpApiClient httpApiClient, string path, TContent content, IDictionary<string, string> headers = null,
																	IDictionary<string, string> queries = null )
		{
			HttpContent httpContent;
			string contentJson;

#if NETCOREAPP2_1
			contentJson = JsonConvert.SerializeObject( content );
#else
			contentJson = JsonSerializer.Serialize( content );
#endif

			httpContent = new StringContent( contentJson );

			return httpApiClient.PatchAsync( path, httpContent, new MediaTypeWithQualityHeaderValue( MediaTypeName ), headers, queries );
		}
#endif
	}
}