﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace Pug.HttpApiClient
{
	public class HttpApiClient : IHttpApiClient
	{
		public Uri BaseAddress { get; }

		public string BasePath { get; }

		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IEnumerable<IHttpClientDecorator> _clientDecorators;
		private readonly IEnumerable<IHttpRequestMessageDecorator> _messageDecorators;

		public Uri BaseUrl { get; }
		
		protected HttpApiClient( Uri baseUrl, IEnumerable<IHttpClientDecorator> clientDecorators = null,
							IEnumerable<IHttpRequestMessageDecorator> messageDecorators = null )
		{
			BaseUrl = baseUrl ?? throw new ArgumentNullException( nameof(baseUrl) );

			BaseAddress =  new Uri( 
					baseUrl.AbsolutePath == "/" ? 
						baseUrl.ToString() : baseUrl.ToString().Replace( baseUrl.AbsolutePath, string.Empty )  
				);
			
			_clientDecorators = clientDecorators ?? Array.Empty<IHttpClientDecorator>();
 			_messageDecorators = messageDecorators ?? Array.Empty<IHttpRequestMessageDecorator>();
			BasePath = baseUrl.AbsolutePath.TrimEnd('/');
		}

		public HttpApiClient( Uri baseUrl, IHttpClientFactory httpClientFactory, IEnumerable<IHttpClientDecorator> clientDecorators = null,
							IEnumerable<IHttpRequestMessageDecorator> messageDecorators = null )
		: this(baseUrl, clientDecorators, messageDecorators)
		{
			_httpClientFactory = httpClientFactory ?? throw new ArgumentNullException( nameof(httpClientFactory) );
		}

		public HttpApiClient( string baseUrl, IHttpClientFactory httpClientFactory, IEnumerable<IHttpClientDecorator> clientDecorators = null,
							IEnumerable<IHttpRequestMessageDecorator> messageDecorators = null )
			: this( new Uri( baseUrl ), httpClientFactory, clientDecorators, messageDecorators )
		{

		}

		private async Task<HttpClient> CreateHttpClientAsync()
		{
			HttpClient httpClient;

			httpClient = _httpClientFactory.CreateClient();

			if( httpClient.BaseAddress is null )
			{
				httpClient.BaseAddress = BaseAddress;
			}

			if( _clientDecorators is null )
				return httpClient;

			DecorationContext decorationContext = new ( httpClient.DefaultRequestHeaders );

			foreach( IHttpClientDecorator clientDecorator in _clientDecorators )
			{
				await clientDecorator.DecorateAsync( decorationContext ).ConfigureAwait( false );
			}

			return httpClient;
		}

		private Uri ConstructRequestUri( string path, IEnumerable<KeyValuePair<string, string>> queries )
		{
			UriBuilder uriBuilder = new ( BaseAddress )
			{
				Path = string.IsNullOrWhiteSpace( path )? BasePath : $"{BasePath}/{ path.TrimStart('/') }",
				Query = queries?.Select( x => $"{WebUtility.UrlEncode( x.Key )}={WebUtility.UrlEncode( x.Value )}" )
								.Aggregate( ( x, y ) => $"{x}&{y}" ) ?? string.Empty
			};

			return uriBuilder.Uri;
		}

		private static HttpRequestMessage CreateHttpRequestMessage( HttpMethod httpMethod, MediaTypeWithQualityHeaderValue mediaType,
																	HttpContent content )
		{
			HttpRequestMessage requestMessage = new ( httpMethod, string.Empty );

			requestMessage.Content = content;

			if( mediaType is not null )
				requestMessage.Headers.Accept.Add( mediaType );

			return requestMessage;
		}

		private async Task<HttpRequestMessage> CreateRequestMessageAsync( HttpMethod httpMethod, string path, IDictionary<string, string> queries,
																		IDictionary<string, string> headers,
																		MediaTypeWithQualityHeaderValue mediaType, HttpContent content )
		{
			HttpRequestMessage requestMessage = CreateHttpRequestMessage( httpMethod, mediaType, content );

			IEnumerable<KeyValuePair<string, string>> uriQueries = queries;
			
			foreach( IHttpRequestMessageDecorator messageDecorator in _messageDecorators )
			{
				MessageDecorationContext messageDecorationContext = new ( requestMessage.Headers );

				await messageDecorator.DecorateAsync( messageDecorationContext ).ConfigureAwait( false );

				if( messageDecorationContext.UrlQueries is not null )
					uriQueries = queries is null ? messageDecorationContext.UrlQueries : queries.Union( messageDecorationContext.UrlQueries );
			}

			if( headers is not null)
				foreach( KeyValuePair<string,string> header in headers )
				{
					if( requestMessage.Headers.Contains( header.Key ) )
					{
						requestMessage.Headers.Remove( header.Key );
					}

					requestMessage.Headers.Add( header.Key, header.Value );
				}

			requestMessage.RequestUri = ConstructRequestUri( path, uriQueries );
			return requestMessage;
		}

		protected virtual async Task<HttpResponseMessage> SendAsync( HttpMethod httpMethod, string path, IDictionary<string, string> queries,
																	IDictionary<string, string> headers, MediaTypeWithQualityHeaderValue mediaType,
																	HttpContent content = null )
		{
			// ReSharper disable once ConvertToUsingDeclaration
			using( HttpClient client = await CreateHttpClientAsync() )
			{
				HttpRequestMessage requestMessage = await CreateRequestMessageAsync( httpMethod, path, queries, headers, mediaType, content );

				HttpResponseMessage responseMessage = await client.SendAsync( requestMessage );

				if( responseMessage.IsSuccessStatusCode )
					return responseMessage;

				switch( responseMessage.StatusCode )
				{
					case HttpStatusCode.Forbidden:
						throw new Application.Security.NotAuthorized( responseMessage.ReasonPhrase );

					case HttpStatusCode.Gone:
						throw new UnknownResourceException( responseMessage );

					case HttpStatusCode.Unauthorized:
						throw new AuthenticationException();

					case HttpStatusCode.BadGateway:
						throw new HttpApiRequestException( responseMessage );

					case HttpStatusCode.BadRequest:
						throw new HttpApiRequestException( responseMessage );
					
					case HttpStatusCode.NotFound:
						throw new UnknownResourceException( responseMessage );

					case HttpStatusCode.MethodNotAllowed:
						throw new HttpApiRequestException(
								new InvalidOperationException( $"{requestMessage.Method.ToString()} method not allowed on resource" ),
								responseMessage
							);

					case HttpStatusCode.Conflict:
						throw new HttpApiRequestException( "Possible authentication/authorization or resource conflict error", responseMessage );

#if !NETSTANDARD
					case HttpStatusCode.Locked:
						throw new HttpApiRequestException(
							new InvalidOperationException( 
								$"Resource cannot be modified: {responseMessage.StatusCode} ({responseMessage.ReasonPhrase})" ),
							responseMessage );
					
					case HttpStatusCode.InsufficientStorage:
						throw new InternalServerErrorException( responseMessage );
#endif

					case HttpStatusCode.NotImplemented:
						throw new HttpApiRequestException( new NotImplementedException(), responseMessage );

					case HttpStatusCode.InternalServerError:
						throw new InternalServerErrorException( responseMessage );
				}

				return responseMessage;
			}
		}
		
		/// <summary>
		/// Invoke HTTP GET call
		/// </summary>
		/// <param name="path">Request path</param>
		/// <param name="mediaType">HTTP media type</param>
		/// <param name="headers">HTTP headers</param>
		/// <param name="queries">URL Queries</param>
		/// <exception cref="Pug.Application.Security.NotAuthorized">When server returned 403 (Forbidden)</exception>
		/// <exception cref="Pug.HttpApiClient.UnknownResourceException">When server returned 404 (Not Found) or 410 (Gone)</exception>
		/// <exception cref="System.Security.Authentication.AuthenticationException">When server returned 401 (Unauthorized)</exception>
		/// <exception cref="Pug.HttpApiClient.HttpApiRequestException">When server returned 40x. InvalidOperationException inner exception will be specified when server returned 405 (Method Not Allowed) or 423 (Locked).</exception>
		/// <exception cref="Pug.HttpApiClient.InternalServerErrorException">When server returned 500 (Internal Server Error) or 507 (Insufficient Storage)</exception>
		/// <returns>Instance of HttpResponseMessage</returns>
		public virtual async Task<HttpResponseMessage> GetAsync( string path,
																MediaTypeWithQualityHeaderValue mediaType,
																IDictionary<string, string> headers,
																IDictionary<string, string> queries )
		{
			return await SendAsync( HttpMethod.Get, path, queries, headers, mediaType );
		}

		/// <summary>
		/// Invoke HTTP POST call
		/// </summary>
		/// <param name="path">Request path</param>
		/// <param name="content">HTTP content</param>
		/// <param name="mediaType">HTTP media type</param>
		/// <param name="headers">HTTP headers</param>
		/// <param name="queries">URL Queries</param>
		/// <exception cref="Pug.Application.Security.NotAuthorized">When server returned 403 (Forbidden)</exception>
		/// <exception cref="Pug.HttpApiClient.UnknownResourceException">When server returned 404 (Not Found) or 410 (Gone)</exception>
		/// <exception cref="System.Security.Authentication.AuthenticationException">When server returned 401 (Unauthorized)</exception>
		/// <exception cref="Pug.HttpApiClient.HttpApiRequestException">When server returned 40x. InvalidOperationException inner exception will be specified when server returned 405 (Method Not Allowed) or 423 (Locked).</exception>
		/// <exception cref="Pug.HttpApiClient.InternalServerErrorException">When server returned 500 (Internal Server Error) or 507 (Insufficient Storage)</exception>
		/// <returns>Instance of HttpResponseMessage</returns>
		public virtual async Task<HttpResponseMessage> PostAsync( string path, HttpContent content,
																MediaTypeWithQualityHeaderValue mediaType, IDictionary<string, string> headers = null,
																IDictionary<string, string> queries = null )
		{
			return await SendAsync( HttpMethod.Post, path, queries, headers, mediaType, content );
		}

		/// <summary>
		/// Invoke HTTP PUT call
		/// </summary>
		/// <param name="path">Request path</param>
		/// <param name="content">HTTP content</param>
		/// <param name="mediaType">HTTP media type</param>
		/// <param name="headers">HTTP headers</param>
		/// <param name="queries">URL Queries</param>
		/// <exception cref="Pug.Application.Security.NotAuthorized">When server returned 403 (Forbidden)</exception>
		/// <exception cref="Pug.HttpApiClient.UnknownResourceException">When server returned 404 (Not Found) or 410 (Gone)</exception>
		/// <exception cref="System.Security.Authentication.AuthenticationException">When server returned 401 (Unauthorized)</exception>
		/// <exception cref="Pug.HttpApiClient.HttpApiRequestException">When server returned 40x. InvalidOperationException inner exception will be specified when server returned 405 (Method Not Allowed) or 423 (Locked).</exception>
		/// <exception cref="Pug.HttpApiClient.InternalServerErrorException">When server returned 500 (Internal Server Error) or 507 (Insufficient Storage)</exception>
		/// <returns>Instance of HttpResponseMessage</returns>
		public virtual async Task<HttpResponseMessage> PutAsync( string path, HttpContent content,
																MediaTypeWithQualityHeaderValue mediaType, IDictionary<string, string> headers,
																IDictionary<string, string> queries )
		{
			return await SendAsync( HttpMethod.Put, path, queries, headers, mediaType, content );
		}

		/// <summary>
		/// Invoke HTTP DELETE call
		/// </summary>
		/// <param name="path">Request path</param>
		/// <param name="mediaType">HTTP media type</param>
		/// <param name="headers">HTTP headers</param>
		/// <param name="queries">URL Queries</param>
		/// <exception cref="Pug.Application.Security.NotAuthorized">When server returned 403 (Forbidden)</exception>
		/// <exception cref="Pug.HttpApiClient.UnknownResourceException">When server returned 404 (Not Found) or 410 (Gone)</exception>
		/// <exception cref="System.Security.Authentication.AuthenticationException">When server returned 401 (Unauthorized)</exception>
		/// <exception cref="Pug.HttpApiClient.HttpApiRequestException">When server returned 40x. InvalidOperationException inner exception will be specified when server returned 405 (Method Not Allowed) or 423 (Locked).</exception>
		/// <exception cref="Pug.HttpApiClient.InternalServerErrorException">When server returned 500 (Internal Server Error) or 507 (Insufficient Storage)</exception>
		/// <returns>Instance of HttpResponseMessage</returns>
		public virtual async Task<HttpResponseMessage> DeleteAsync( string path,
																	IDictionary<string, string> headers = null,
																	IDictionary<string, string> queries = null,
																	MediaTypeWithQualityHeaderValue mediaType = null )
		{
			return await SendAsync( HttpMethod.Delete, path, queries, headers, mediaType );
		}

		/// <summary>
		/// Invoke HTTP PATCH call
		/// </summary>
		/// <param name="path">Request path</param>
		/// <param name="content">HTTP content</param>
		/// <param name="mediaType">HTTP media type</param>
		/// <param name="headers">HTTP headers</param>
		/// <param name="queries">URL Queries</param>
		/// <exception cref="Pug.Application.Security.NotAuthorized">When server returned 403 (Forbidden)</exception>
		/// <exception cref="Pug.HttpApiClient.UnknownResourceException">When server returned 404 (Not Found) or 410 (Gone)</exception>
		/// <exception cref="System.Security.Authentication.AuthenticationException">When server returned 401 (Unauthorized)</exception>
		/// <exception cref="Pug.HttpApiClient.HttpApiRequestException">When server returned 40x. InvalidOperationException inner exception will be specified when server returned 405 (Method Not Allowed) or 423 (Locked).</exception>
		/// <exception cref="Pug.HttpApiClient.InternalServerErrorException">When server returned 500 (Internal Server Error) or 507 (Insufficient Storage)</exception>
		/// <returns>Instance of HttpResponseMessage</returns>
		public virtual async Task<HttpResponseMessage> PatchAsync( string path,
																	HttpContent content,
																	MediaTypeWithQualityHeaderValue mediaType, 
																	IDictionary<string, string> headers,
																	IDictionary<string, string> queries )
		{
			HttpMethod httpMethod = null;
#if NETSTANDARD
			httpMethod = new HttpMethod( "PATCH" );
#else
			httpMethod = HttpMethod.Patch;
#endif
			return await SendAsync(httpMethod, path, queries, headers, mediaType, content );
		}
	}
}