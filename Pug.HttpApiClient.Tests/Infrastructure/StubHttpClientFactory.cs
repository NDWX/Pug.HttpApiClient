using System;
using System.Net.Http;
using System.Threading;

namespace Pug.HttpApiClient.Tests.Infrastructure
{
	/// <summary>
	/// <see cref="IHttpClientFactory"/> over a single shared handler.
	/// </summary>
	/// <remarks>
	/// A NEW <see cref="HttpClient"/> is handed out per call, over a handler created with
	/// <c>disposeHandler: false</c>. This is not incidental: <c>HttpApiClient.SendAsync</c> wraps the client
	/// in a <c>using</c> block and therefore disposes it after EVERY request. Returning a cached client would
	/// make the second request throw <see cref="ObjectDisposedException"/>, and letting the client own the
	/// handler would take the request log down with it.
	/// </remarks>
	public sealed class StubHttpClientFactory : IHttpClientFactory
	{
		private readonly HttpMessageHandler _handler;
		private int _createClientCount;

		public StubHttpClientFactory( HttpMessageHandler handler )
		{
			_handler = handler ?? throw new ArgumentNullException( nameof(handler) );
		}

		public int CreateClientCount => Volatile.Read( ref _createClientCount );

		public HttpClient CreateClient( string name )
		{
			Interlocked.Increment( ref _createClientCount );

			return new HttpClient( _handler, disposeHandler: false );
		}
	}
}
