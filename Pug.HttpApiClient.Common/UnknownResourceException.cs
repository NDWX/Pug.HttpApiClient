using System;
using System.Net.Http;
using System.Runtime.Serialization;

namespace Pug.HttpApiClient
{
	[Serializable]
	public class UnknownResourceException : HttpApiRequestException
	{

		public UnknownResourceException( Exception inner ) 
			: base( inner )
		{
		}

		public UnknownResourceException( HttpResponseMessage httpResponseMessage ) : base(httpResponseMessage)
		{
		}

		protected UnknownResourceException( SerializationInfo info, StreamingContext context )
			: base( info, context )
		{

		}
	}
}