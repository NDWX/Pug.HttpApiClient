using System;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Pug.HttpApiClient
{
	public sealed class BasicAuthenticationMessageDecorator : AuthorizationHeaderDecorator
	{
		public BasicAuthenticationMessageDecorator( string username, string password )
			: base( "Basic", Helpers.HttpBase64Encode( $"{username}:{password}" ) )
		{
		}
	}
}