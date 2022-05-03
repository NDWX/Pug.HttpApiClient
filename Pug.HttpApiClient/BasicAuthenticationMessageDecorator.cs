using System;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Pug.HttpApiClient
{
	public sealed class BasicAuthenticationMessageDecorator : AuthorizationHeaderDecorator
	{
		public BasicAuthenticationMessageDecorator( string username, string password )
		: base("BASIC", Convert.ToBase64String( Encoding.UTF8.GetBytes( $"{username}:{password}" ) ) )
		{
		}
	}
}