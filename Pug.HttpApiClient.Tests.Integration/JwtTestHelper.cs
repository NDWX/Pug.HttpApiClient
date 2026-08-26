using System;
using System.Text.Json;

namespace Pug.HttpApiClient.Tests.Integration
{
	/// <summary>
	/// Minimal JWT payload decoder used to assert on real claims returned by Keycloak. Does not
	/// validate the signature - these tests are about the claims a real provider puts in the token,
	/// not about re-implementing JWT validation.
	/// </summary>
	internal static class JwtTestHelper
	{
		public static JsonElement DecodePayload( string jwt )
		{
			if( string.IsNullOrWhiteSpace( jwt ) ) throw new ArgumentException( "Value cannot be null or whitespace.", nameof(jwt) );

			string[] segments = jwt.Split( '.' );

			if( segments.Length != 3 )
				throw new ArgumentException( $"Not a JWT: expected 3 dot-separated segments, found {segments.Length}.", nameof(jwt) );

			byte[] payloadBytes = Base64UrlDecode( segments[1] );

			using JsonDocument document = JsonDocument.Parse( payloadBytes );

			return document.RootElement.Clone();
		}

		private static byte[] Base64UrlDecode( string value )
		{
			string base64 = value.Replace( '-', '+' ).Replace( '_', '/' );

			switch( base64.Length % 4 )
			{
				case 2: base64 += "=="; break;
				case 3: base64 += "="; break;
			}

			return Convert.FromBase64String( base64 );
		}
	}
}
