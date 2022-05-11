using System;
using System.Text;

namespace Pug.HttpApiClient
{
	public static class Helpers
	{
		private static readonly Encoding HttpHeaderEncoding = Encoding.GetEncoding( "ISO-8859-1" );

		public static string HttpBase64Encode( string text )
		{
			return Convert.ToBase64String( HttpHeaderEncoding.GetBytes( text ) );
		}
	}
}