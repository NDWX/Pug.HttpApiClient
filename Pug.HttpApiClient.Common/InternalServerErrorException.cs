using System;
using System.Net.Http;
using System.Runtime.Serialization;

namespace Pug.HttpApiClient
{
	[Serializable]
	public class InternalServerErrorException : HttpApiRequestException
	{
		public InternalServerErrorException( HttpResponseMessage httpResponseMessage ) : base( httpResponseMessage )
		{
		}

		public InternalServerErrorException( string message, HttpResponseMessage httpResponseMessage ) : base( message, httpResponseMessage )
		{
		}

		protected InternalServerErrorException( SerializationInfo info, StreamingContext context ) : base( info, context )
		{
		}
	}
}