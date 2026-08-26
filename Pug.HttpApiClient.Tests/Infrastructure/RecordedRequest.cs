using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Pug.HttpApiClient.Tests.Infrastructure
{
	/// <summary>
	/// Immutable snapshot of a request as it reached the transport, taken before the
	/// underlying <see cref="HttpRequestMessage"/> (and its content) can be disposed.
	/// </summary>
	public sealed class RecordedRequest
	{
		public HttpMethod Method { get; }

		public Uri RequestUri { get; }

		/// <summary>Raw request body, or <c>null</c> when the request carried no content.</summary>
		public string Body { get; }

		/// <summary>Value of the <c>Authorization</c> header, or <c>null</c>.</summary>
		public string AuthorizationScheme { get; }

		public string AuthorizationParameter { get; }

		/// <summary>Media types listed in the <c>Accept</c> header, in order.</summary>
		public IReadOnlyList<string> Accept { get; }

		public string ContentType { get; }

		private readonly IDictionary<string, string[]> _headers;

		public RecordedRequest( HttpRequestMessage request, string body )
		{
			Method = request.Method;
			RequestUri = request.RequestUri;
			Body = body;

			AuthorizationScheme = request.Headers.Authorization?.Scheme;
			AuthorizationParameter = request.Headers.Authorization?.Parameter;
			Accept = request.Headers.Accept.Select( x => x.MediaType ).ToArray();
			ContentType = request.Content?.Headers.ContentType?.ToString();

			_headers = request.Headers.ToDictionary( x => x.Key, x => x.Value.ToArray(), StringComparer.OrdinalIgnoreCase );
		}

		/// <summary>Query string of the request URI without the leading '?'.</summary>
		public string Query => RequestUri.Query.TrimStart( '?' );

		public string AbsolutePath => RequestUri.AbsolutePath;

		public bool HasHeader( string name ) => _headers.ContainsKey( name );

		public string GetHeader( string name ) =>
			_headers.TryGetValue( name, out string[] values ) ? string.Join( ",", values ) : null;

		/// <summary>
		/// Parse an <c>application/x-www-form-urlencoded</c> body into its fields.
		/// </summary>
		public IDictionary<string, string> FormFields()
		{
			Dictionary<string, string> fields = new ( StringComparer.Ordinal );

			if( string.IsNullOrEmpty( Body ) )
				return fields;

			foreach( string pair in Body.Split( '&' ) )
			{
				if( pair.Length == 0 )
					continue;

				int separatorIndex = pair.IndexOf( '=' );

				if( separatorIndex < 0 )
					fields[Uri.UnescapeDataString( pair )] = string.Empty;
				else
					fields[Uri.UnescapeDataString( pair.Substring( 0, separatorIndex ) )] =
						Uri.UnescapeDataString( pair.Substring( separatorIndex + 1 ).Replace( "+", " " ) );
			}

			return fields;
		}

		public override string ToString() => $"{Method} {RequestUri}";
	}
}
