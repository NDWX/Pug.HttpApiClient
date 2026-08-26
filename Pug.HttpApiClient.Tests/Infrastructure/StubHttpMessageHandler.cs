using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Pug.HttpApiClient.Tests.Infrastructure
{
	/// <summary>
	/// Transport stub. Answers each request from a responder delegate and keeps an ordered,
	/// thread-safe log of what it was asked for.
	/// </summary>
	public sealed class StubHttpMessageHandler : HttpMessageHandler
	{
		private readonly Func<RecordedRequest, HttpResponseMessage> _responder;
		private readonly List<RecordedRequest> _requests = new ();
		private readonly object _sync = new ();

		public StubHttpMessageHandler( Func<RecordedRequest, HttpResponseMessage> responder )
		{
			_responder = responder ?? throw new ArgumentNullException( nameof(responder) );
		}

		/// <summary>Always answer with this status and body, whatever is asked.</summary>
		public static StubHttpMessageHandler AlwaysReturns( HttpStatusCode statusCode, string body = "",
															string contentType = "application/json" ) =>
			new ( _ => Responses.Create( statusCode, body, contentType ) );

		public IReadOnlyList<RecordedRequest> Requests
		{
			get
			{
				lock( _sync ) return _requests.ToArray();
			}
		}

		public RecordedRequest LastRequest
		{
			get
			{
				lock( _sync ) return _requests.Count == 0 ? null : _requests[_requests.Count - 1];
			}
		}

		public int RequestCount
		{
			get
			{
				lock( _sync ) return _requests.Count;
			}
		}

		protected override async Task<HttpResponseMessage> SendAsync( HttpRequestMessage request, CancellationToken cancellationToken )
		{
			// The body must be captured here: HttpClient disposes request content once the send completes,
			// so reading it from the test afterwards would throw ObjectDisposedException.
			string body = request.Content is null ? null : await request.Content.ReadAsStringAsync().ConfigureAwait( false );

			RecordedRequest recorded = new ( request, body );

			lock( _sync ) _requests.Add( recorded );

			HttpResponseMessage response = _responder( recorded );
			response.RequestMessage = request;

			return response;
		}
	}
}
