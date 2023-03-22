using System;
using System.Runtime.Serialization;

namespace Pug.HttpApiClient.OAuth2
{
	[Serializable]
	public class AccessTokenException : Exception
	{
		public AccessTokenException()
		{
		}
		
		protected AccessTokenException( SerializationInfo info, StreamingContext context )
		: base(info, context)
		{
		}
	}
}