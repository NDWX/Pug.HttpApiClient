using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
#if !NETSTANDARD
using System.Net.Mime;
#endif
#if NETCOREAPP2_1 || NETSTANDARD
using Newtonsoft.Json;
#else
using System.Text.Json;
#endif

namespace Pug.HttpApiClient.Json
{
	/// <summary>
	/// JSON extension functions for IHttpApiClientJsonExtensions
	/// </summary>
	public static class IHttpApiClientJsonExtensions
	{
		private const string MediaTypeName =
#if NETSTANDARD
			"application/json";
#else
			MediaTypeNames.Application.Json;
#endif
		private static readonly MediaTypeWithQualityHeaderValue MediaType = new ( MediaTypeName );
		
#if NETCOREAPP2_1 || NETSTANDARD
		private static JsonSerializer CreateJsonSerializer(JsonSerializerSettings settings)
		{
			return settings is null ? JsonSerializer.CreateDefault() : JsonSerializer.Create( settings );
		}
#else
		private static JsonSerializerOptions defaultJsonSerializerOptions = new ()
		{
			PropertyNameCaseInsensitive = true
		};

		/// <summary>
		/// Default JsonSerializerOptions used for JSON serialization/deserialization
		/// </summary>
		public static JsonSerializerOptions DefaultJsonSerializerOptions => defaultJsonSerializerOptions;

		/// <summary>
		/// Set default JsonSerializerOptions used for JSON serialization/deserialization
		/// </summary>
		/// <param name="httpApiClient"></param>
		/// <param name="jsonSerializerOptions"></param>
		public static void SetJsonSerializerOptions(this IHttpApiClient httpApiClient, JsonSerializerOptions jsonSerializerOptions )
		{
			defaultJsonSerializerOptions = jsonSerializerOptions;
		}
#endif
		
		private static HttpContent CreateHttpContent<TContent>( 
			TContent content, 
#if NETCOREAPP2_1 || NETSTANDARD 
			JsonSerializerSettings jsonSerializerSettings = null
#else
			JsonSerializerOptions jsonSerializerOptions = null
#endif
			)
		{
			string contentJson;

#if NETCOREAPP2_1 || NETSTANDARD
			contentJson = JsonConvert.SerializeObject( content );
#else
			contentJson = JsonSerializer.Serialize( content, defaultJsonSerializerOptions?? jsonSerializerOptions );
#endif
			HttpContent httpContent = new StringContent( contentJson );
			httpContent.Headers.ContentType = MediaType;
			
			return httpContent;
		}

		private static async Task<TResult> CheckAndDeserializeAsync<TResult>( 
			HttpResponseMessage response, 
#if NETCOREAPP2_1 || NETSTANDARD 
			JsonSerializerSettings jsonSerializerSettings = null
#else
			JsonSerializerOptions jsonSerializerOptions = null
		#endif
		)
		{
			if( !response.IsSuccessStatusCode )
				throw new HttpApiRequestException( response );

#if NETCOREAPP2_1 || NETSTANDARD
			using Stream contentStream = await response.Content.ReadAsStreamAsync();
			using StreamReader streamReader = new ( contentStream );

			return CreateJsonSerializer( jsonSerializerSettings )
								.Deserialize<TResult>(
										new JsonTextReader(
												streamReader
											)
									);
#else
			await using Stream contentStream = await response.Content.ReadAsStreamAsync();

			return await JsonSerializer.DeserializeAsync<TResult>( 
						contentStream, jsonSerializerOptions?? 
										defaultJsonSerializerOptions );
#endif
		}

		/// <summary>
		/// Invoke GET call with JSON request and response type.
		/// </summary>
		/// <param name="httpApiClient">Instance of IHttpApiClient</param>
		/// <param name="path">Request path</param>
		/// <param name="headers">HTTP headers</param>
		/// <param name="queries">URL Queries</param>
#if NETCOREAPP2_1 || NETSTANDARD 
		/// <param name="jsonSerializerSettings">JSON serializer settings</param>
#else
		/// <param name="jsonSerializerOptions">JSON serializer options</param>
#endif
		/// <typeparam name="T">Serializable class instance</typeparam>
		/// <returns>Instance of <typeparamref name="T"/></returns>
		/// <exception cref="Pug.Application.Security.NotAuthorized">When server returned 403 (Forbidden)</exception>
		/// <exception cref="Pug.HttpApiClient.UnknownResourceException">When server returned 404 (Not Found) or 410 (Gone)</exception>
		/// <exception cref="System.Security.Authentication.AuthenticationException">When server returned 401 (Unauthorized)</exception>
		/// <exception cref="Pug.HttpApiClient.HttpApiRequestException">When server returned 40x. InvalidOperationException inner exception will be specified when server returned 405 (Method Not Allowed) or 423 (Locked).</exception>
		/// <exception cref="Pug.HttpApiClient.InternalServerErrorException">When server returned 500 (Internal Server Error) or 507 (Insufficient Storage)</exception>
		public static async Task<T> GetAsync<T>( 
			this IHttpApiClient httpApiClient, string path, IDictionary<string, string> headers = null,
			IDictionary<string, string> queries = null, 
#if NETCOREAPP2_1 || NETSTANDARD 
			JsonSerializerSettings jsonSerializerSettings = null
#else
			JsonSerializerOptions jsonSerializerOptions = null
#endif
			)
		{
			HttpResponseMessage response = await httpApiClient.GetAsync( path, MediaType, headers, queries );

#if NETCOREAPP2_1 || NETSTANDARD 
			return await CheckAndDeserializeAsync<T>( response, jsonSerializerSettings );
#else
			return await CheckAndDeserializeAsync<T>( response, jsonSerializerOptions );
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
		public static Task<HttpResponseMessage> PostAsync<TContent>(
			this IHttpApiClient httpApiClient, string path, TContent content,
			IDictionary<string, string> headers = null, IDictionary<string, string> queries = null )
		{
			HttpContent httpContent = CreateHttpContent<TContent>( content );

			return httpApiClient.PostAsync( path, httpContent, MediaType, headers, queries );
		}
		
#if NETCOREAPP2_1 || NETSTANDARD 
		/// <summary>
		/// Invoke POST call with JSON request and response type.
		/// </summary>
		/// <param name="httpApiClient">Instance of IHttpApiClient</param>
		/// <param name="path">Request path</param>
		/// <param name="content">Request content</param>
		/// <param name="headers">HTTP headers</param>
		/// <param name="queries">URL Queries</param>
		/// <param name="jsonSerializerSettings">JSON serializer settings</param>
		/// <typeparam name="TContent">Type of object to POST to server</typeparam>
		/// <typeparam name="TResult">Type of object returned by server</typeparam>
		/// <returns>Instance of <typeparamref name="TResult"/></returns>
		/// <exception cref="Pug.Application.Security.NotAuthorized">When server returned 403 (Forbidden)</exception>
		/// <exception cref="Pug.HttpApiClient.UnknownResourceException">When server returned 404 (Not Found) or 410 (Gone)</exception>
		/// <exception cref="System.Security.Authentication.AuthenticationException">When server returned 401 (Unauthorized)</exception>
		/// <exception cref="Pug.HttpApiClient.HttpApiRequestException">When server returned 40x. InvalidOperationException inner exception will be specified when server returned 405 (Method Not Allowed) or 423 (Locked).</exception>
		/// <exception cref="Pug.HttpApiClient.InternalServerErrorException">When server returned 500 (Internal Server Error) or 507 (Insufficient Storage)</exception>
		public static async Task<TResult> PostAsync<TContent, TResult>(
			this IHttpApiClient httpApiClient, string path, TContent content,
			IDictionary<string, string> headers = null, IDictionary<string, string> queries = null, 
			JsonSerializerSettings jsonSerializerSettings = null
			)
		{
			HttpContent httpContent = CreateHttpContent<TContent>( content, jsonSerializerSettings );

			HttpResponseMessage response = await httpApiClient.PostAsync( path, httpContent, MediaType, headers, queries );

			return await CheckAndDeserializeAsync<TResult>( response, jsonSerializerSettings );

		}
#else
		/// <summary>
		/// Invoke POST call with JSON request and response type.
		/// </summary>
		/// <param name="httpApiClient">Instance of IHttpApiClient</param>
		/// <param name="path">Request path</param>
		/// <param name="content">Request content</param>
		/// <param name="headers">HTTP headers</param>
		/// <param name="queries">URL Queries</param>
		/// <param name="jsonSerializerOptions">JSON serializer options</param>
		/// <typeparam name="TContent">Type of object to POST to server</typeparam>
		/// <typeparam name="TResult">Type of object returned by server</typeparam>
		/// <returns>Instance of <typeparamref name="TResult"/></returns>
		/// <exception cref="Pug.Application.Security.NotAuthorized">When server returned 403 (Forbidden)</exception>
		/// <exception cref="Pug.HttpApiClient.UnknownResourceException">When server returned 404 (Not Found) or 410 (Gone)</exception>
		/// <exception cref="System.Security.Authentication.AuthenticationException">When server returned 401 (Unauthorized)</exception>
		/// <exception cref="Pug.HttpApiClient.HttpApiRequestException">When server returned 40x. InvalidOperationException inner exception will be specified when server returned 405 (Method Not Allowed) or 423 (Locked).</exception>
		/// <exception cref="Pug.HttpApiClient.InternalServerErrorException">When server returned 500 (Internal Server Error) or 507 (Insufficient Storage)</exception>
		public static async Task<TResult> PostAsync<TContent, TResult>(
			this IHttpApiClient httpApiClient, string path, TContent content,
			IDictionary<string, string> headers = null, IDictionary<string, string> queries = null,
			JsonSerializerOptions jsonSerializerOptions = null
		)
		{
			HttpContent httpContent = CreateHttpContent<TContent>( content, jsonSerializerOptions );

			HttpResponseMessage response = await httpApiClient.PostAsync( path, httpContent, MediaType, headers, queries );

			return await CheckAndDeserializeAsync<TResult>( response, jsonSerializerOptions );
		}
#endif

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
		public static Task<HttpResponseMessage> PutAsync<TContent>(
			this IHttpApiClient httpApiClient, string path, TContent content,
			IDictionary<string, string> headers = null, IDictionary<string, string> queries = null )
		{
			HttpContent httpContent = CreateHttpContent<TContent>( content );

			return httpApiClient.PutAsync( path, httpContent, MediaType, headers, queries );
		}

		/// <summary>
		/// Invoke PUT call with JSON request and response type.
		/// </summary>
		/// <param name="httpApiClient">Instance of IHttpApiClient</param>
		/// <param name="path">Request path</param>
		/// <param name="content">Request content</param>
		/// <param name="headers">HTTP headers</param>
		/// <param name="queries">URL Queries</param>
#if NETCOREAPP2_1 || NETSTANDARD 
		/// <param name="jsonSerializerSettings">JSON serializer settings</param>
#else
		/// <param name="jsonSerializerOptions">JSON serializer options</param>
#endif
		/// <typeparam name="TContent">Type of object to PUT to server</typeparam>
		/// <typeparam name="TResult">Type of object returned by server</typeparam>
		/// <returns>Instance of <typeparamref name="TResult"/></returns>
		/// <exception cref="Pug.Application.Security.NotAuthorized">When server returned 403 (Forbidden)</exception>
		/// <exception cref="Pug.HttpApiClient.UnknownResourceException">When server returned 404 (Not Found) or 410 (Gone)</exception>
		/// <exception cref="System.Security.Authentication.AuthenticationException">When server returned 401 (Unauthorized)</exception>
		/// <exception cref="Pug.HttpApiClient.HttpApiRequestException">When server returned 40x. InvalidOperationException inner exception will be specified when server returned 405 (Method Not Allowed) or 423 (Locked).</exception>
		/// <exception cref="Pug.HttpApiClient.InternalServerErrorException">When server returned 500 (Internal Server Error) or 507 (Insufficient Storage)</exception>
		public static async Task<TResult> PutAsync<TContent, TResult>(
			this IHttpApiClient httpApiClient, string path, TContent content,
			IDictionary<string, string> headers = null, IDictionary<string, string> queries = null, 
#if NETCOREAPP2_1 || NETSTANDARD 
			JsonSerializerSettings jsonSerializerSettings = null
#else
			JsonSerializerOptions jsonSerializerOptions = null
#endif
			)
		{
			HttpContent httpContent = CreateHttpContent<TContent>( content );

			HttpResponseMessage response = await httpApiClient.PutAsync( path, httpContent, MediaType, headers, queries );

#if NETCOREAPP2_1 || NETSTANDARD 
			return await CheckAndDeserializeAsync<TResult>( response, jsonSerializerSettings );
#else
			return await CheckAndDeserializeAsync<TResult>( response, jsonSerializerOptions );
#endif
		}

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
		public static Task<HttpResponseMessage> PatchAsync<TContent>(
			this IHttpApiClient httpApiClient, string path, TContent content,
			IDictionary<string, string> headers = null, IDictionary<string, string> queries = null )
		{
			HttpContent httpContent = CreateHttpContent<TContent>( content );

			return httpApiClient.PatchAsync( path, httpContent, MediaType, headers, queries );
		}

		/// <summary>
		/// Invoke PATCH call with JSON request and response type.
		/// </summary>
		/// <param name="httpApiClient">Instance of IHttpApiClient</param>
		/// <param name="path">Request path</param>
		/// <param name="content">Request content</param>
		/// <param name="headers">HTTP headers</param>
		/// <param name="queries">URL Queries</param>
#if NETCOREAPP2_1 || NETSTANDARD 
		/// <param name="jsonSerializerSettings">JSON serializer settings</param>
#else
		/// <param name="jsonSerializerOptions">JSON serializer options</param>
#endif
		/// <typeparam name="TContent">Type of object to send to server</typeparam>
		/// <typeparam name="TResult">Type of object returned by server</typeparam>
		/// <returns>Instance of <typeparamref name="TResult"/></returns>
		/// <exception cref="Pug.Application.Security.NotAuthorized">When server returned 403 (Forbidden)</exception>
		/// <exception cref="Pug.HttpApiClient.UnknownResourceException">When server returned 404 (Not Found) or 410 (Gone)</exception>
		/// <exception cref="System.Security.Authentication.AuthenticationException">When server returned 401 (Unauthorized)</exception>
		/// <exception cref="Pug.HttpApiClient.HttpApiRequestException">When server returned 40x. InvalidOperationException inner exception will be specified when server returned 405 (Method Not Allowed) or 423 (Locked).</exception>
		/// <exception cref="Pug.HttpApiClient.InternalServerErrorException">When server returned 500 (Internal Server Error) or 507 (Insufficient Storage)</exception>
		public static async Task<TResult> PatchAsync<TContent, TResult>(
			this IHttpApiClient httpApiClient, string path, TContent content,
			IDictionary<string, string> headers = null, IDictionary<string, string> queries = null, 
#if NETCOREAPP2_1 || NETSTANDARD 
			JsonSerializerSettings jsonSerializerSettings = null
#else
			JsonSerializerOptions jsonSerializerOptions = null
#endif
			)
		{
			HttpContent httpContent = CreateHttpContent<TContent>( content );

			HttpResponseMessage response = await httpApiClient.PatchAsync( path, httpContent, MediaType, headers, queries );

#if NETCOREAPP2_1 || NETSTANDARD 
			return await CheckAndDeserializeAsync<TResult>( response, jsonSerializerSettings );
#else
			return await CheckAndDeserializeAsync<TResult>( response, jsonSerializerOptions );
#endif
		}
	}
}