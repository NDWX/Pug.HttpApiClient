using System;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;

namespace Pug.HttpApiClient
{
	[Serializable]
	public class HttpApiRequestException : Exception
	{
		private const string ResponseStatusReasonFieldName = "ResponseStatusReason";
		private const string ResponseStatusCodeFieldName = "ResponseStatusCode";
		private const string ResponseMessageFieldName = "ResponseMessage";
		public HttpStatusCode ResponseStatusCode { get; }
		public string ResponseStatusReason { get; }
		public string ResponseMessage { get; }

		public HttpApiRequestException( Exception inner ) 
			: base( string.Empty, inner )
		{
		}

		public HttpApiRequestException( Exception inner,  HttpResponseMessage httpResponseMessage ) 
			: base( string.Empty, inner )
		{
			ResponseStatusCode = httpResponseMessage.StatusCode;
			ResponseStatusReason = httpResponseMessage.ReasonPhrase;
			ResponseMessage = httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait( false ).GetAwaiter().GetResult();
		}

		public HttpApiRequestException( HttpResponseMessage httpResponseMessage )
		{
			ResponseStatusCode = httpResponseMessage.StatusCode;
			ResponseStatusReason = httpResponseMessage.ReasonPhrase;
			ResponseMessage = httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait( false ).GetAwaiter().GetResult();
		}

		public HttpApiRequestException( string message, HttpResponseMessage httpResponseMessage ) : base( message )
		{
			ResponseStatusCode = httpResponseMessage.StatusCode;
			ResponseStatusReason = httpResponseMessage.ReasonPhrase;
			ResponseMessage = httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait( false ).GetAwaiter().GetResult();
		}

		protected HttpApiRequestException( SerializationInfo info, StreamingContext context ) : base(info, context)
		{
			ResponseStatusCode = (HttpStatusCode)Convert.ToInt32( info.GetString( ResponseMessageFieldName ) );
			ResponseStatusReason = info.GetString( ResponseStatusReasonFieldName );
			ResponseMessage = info.GetString( ResponseMessageFieldName );
		}

		public override void GetObjectData( SerializationInfo info, StreamingContext context )
		{
			base.GetObjectData( info, context );
			
			info.AddValue( ResponseStatusCodeFieldName, ((int)ResponseStatusCode).ToString() );
			info.AddValue( ResponseStatusReasonFieldName, ResponseStatusReason );
			info.AddValue( ResponseMessageFieldName, ResponseMessage );
		}
	}
}