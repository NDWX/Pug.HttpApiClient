using System;
using System.Text;
using System.Threading.Tasks;
using Pug.HttpApiClient.Tests.Infrastructure;
using Xunit;

namespace Pug.HttpApiClient.Tests.Client
{
	public class HelpersTests
	{
		[Fact]
		public void HttpBase64Encode_AsciiVector_ReturnsExpectedBase64()
		{
			string result = Helpers.HttpBase64Encode( "user:pass" );

			Assert.Equal( "dXNlcjpwYXNz", result );
		}

		[Fact]
		public void HttpBase64Encode_Iso8859_1CharacterDiffersFromUtf8_UsesIso8859_1()
		{
			// 'é' (U+00E9) is a single byte (0xE9) in ISO-8859-1 but two bytes in UTF-8 (0xC3 0xA9),
			// so the two encodings produce different Base64 output ("6Q==" vs "w6k=").
			string result = Helpers.HttpBase64Encode( "é" );

			Assert.Equal( "6Q==", result );
			Assert.NotEqual( Convert.ToBase64String( Encoding.UTF8.GetBytes( "é" ) ), result );
		}

		[Fact]
		public void HttpBase64Encode_EmptyString_ReturnsEmptyString()
		{
			string result = Helpers.HttpBase64Encode( string.Empty );

			Assert.Equal( string.Empty, result );
		}

		[Fact]
		public async Task BasicAuthenticationMessageDecorator_Decorate_SetsBasicAuthorizationHeader()
		{
			StubHttpMessageHandler handler = new ( _ => Responses.Json( "{}" ) );
			StubHttpClientFactory factory = new ( handler );
			BasicAuthenticationMessageDecorator decorator = new ( "user", "pass" );
			Pug.HttpApiClient.HttpApiClient client = new (
				new Uri( "https://h/v1" ), factory, null, new IHttpRequestMessageDecorator[] { decorator } );

			await client.GetAsync( "widgets", null, null, null );

			Assert.Equal( "Basic", handler.LastRequest.AuthorizationScheme );
			Assert.Equal( "dXNlcjpwYXNz", handler.LastRequest.AuthorizationParameter );
		}
	}
}
