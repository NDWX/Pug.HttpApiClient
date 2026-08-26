using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Pug.HttpApiClient.Tests.Infrastructure;
using Xunit;

namespace Pug.HttpApiClient.Tests.Client
{
	public class HttpApiClientTests
	{
		[Theory]
		[InlineData( "https://h/", "https://h/", "" )]
		[InlineData( "https://h/v1", "https://h/", "/v1" )]
		[InlineData( "https://h/v1/", "https://h/", "/v1" )]
		[InlineData( "https://h:8443/v1", "https://h:8443/", "/v1" )]
		// Fix A: GetLeftPart( UriPartial.Authority ) must not be mangled by the first path
		// segment coincidentally matching a substring of the authority.
		[InlineData( "https://auth.example.com/auth", "https://auth.example.com/", "/auth" )]
		public void Ctor_UriDerivation_ProducesExpectedBaseAddressAndBasePath( string baseUrl, string expectedBaseAddress,
																			string expectedBasePath )
		{
			Uri uri = new ( baseUrl );
			StubHttpClientFactory factory = new ( new StubHttpMessageHandler( _ => Responses.Json( "{}" ) ) );

			Pug.HttpApiClient.HttpApiClient client = new ( uri, factory );

			Assert.Equal( expectedBaseAddress, client.BaseAddress.ToString() );
			Assert.Equal( expectedBasePath, client.BasePath );
			Assert.Equal( uri, client.BaseUrl );
		}

		[Fact]
		public async Task ConstructRequestUri_LeadingAndNoLeadingSlashPath_ProduceSameUri()
		{
			StubHttpMessageHandler handler = new ( _ => Responses.Json( "{}" ) );
			StubHttpClientFactory factory = new ( handler );
			Pug.HttpApiClient.HttpApiClient client = new ( new Uri( "https://h/v1" ), factory );

			await client.GetAsync( "/foo/bar", null, null, null );
			Uri withLeadingSlash = handler.Requests[0].RequestUri;

			await client.GetAsync( "foo/bar", null, null, null );
			Uri withoutLeadingSlash = handler.Requests[1].RequestUri;

			Assert.Equal( withLeadingSlash, withoutLeadingSlash );
			Assert.Equal( "/v1/foo/bar", withLeadingSlash.AbsolutePath );
		}

		[Theory]
		[InlineData( null )]
		[InlineData( "" )]
		[InlineData( "   " )]
		public async Task ConstructRequestUri_NullEmptyOrWhitespacePath_FallsBackToBasePath( string path )
		{
			StubHttpMessageHandler handler = new ( _ => Responses.Json( "{}" ) );
			StubHttpClientFactory factory = new ( handler );
			Pug.HttpApiClient.HttpApiClient client = new ( new Uri( "https://h/v1" ), factory );

			await client.GetAsync( path, null, null, null );

			Assert.Equal( "/v1", handler.LastRequest.AbsolutePath );
		}

		[Fact]
		public async Task ConstructRequestUri_SingleQuery_AppendsQueryString()
		{
			StubHttpMessageHandler handler = new ( _ => Responses.Json( "{}" ) );
			StubHttpClientFactory factory = new ( handler );
			Pug.HttpApiClient.HttpApiClient client = new ( new Uri( "https://h/v1" ), factory );

			await client.GetAsync( "foo", null, null, new Dictionary<string, string> { ["a"] = "1" } );

			Assert.Equal( "a=1", handler.LastRequest.Query );
		}

		[Fact]
		public async Task ConstructRequestUri_MultipleQueries_JoinedWithAmpersand()
		{
			StubHttpMessageHandler handler = new ( _ => Responses.Json( "{}" ) );
			StubHttpClientFactory factory = new ( handler );
			Pug.HttpApiClient.HttpApiClient client = new ( new Uri( "https://h/v1" ), factory );

			IDictionary<string, string> queries = new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" };

			await client.GetAsync( "foo", null, null, queries );

			Assert.Equal( "a=1&b=2", handler.LastRequest.Query );
		}

		[Theory]
		[InlineData( "a b" )]
		[InlineData( "a&b" )]
		[InlineData( "a=b" )]
		[InlineData( "é" )]
		public async Task ConstructRequestUri_QueryValuesNeedingEncoding_AreWebUtilityUrlEncoded( string value )
		{
			StubHttpMessageHandler handler = new ( _ => Responses.Json( "{}" ) );
			StubHttpClientFactory factory = new ( handler );
			Pug.HttpApiClient.HttpApiClient client = new ( new Uri( "https://h/v1" ), factory );

			await client.GetAsync( "foo", null, null, new Dictionary<string, string> { ["q"] = value } );

			string expected = $"q={WebUtility.UrlEncode( value )}";
			Assert.Equal( expected, handler.LastRequest.Query );
		}

		[Fact]
		public async Task ConstructRequestUri_NullQueries_ProducesEmptyQueryString()
		{
			StubHttpMessageHandler handler = new ( _ => Responses.Json( "{}" ) );
			StubHttpClientFactory factory = new ( handler );
			Pug.HttpApiClient.HttpApiClient client = new ( new Uri( "https://h/v1" ), factory );

			await client.GetAsync( "foo", null, null, null );

			Assert.Equal( string.Empty, handler.LastRequest.Query );
		}

		[Fact]
		public async Task GetAsync_SendsGetWithAcceptHeader()
		{
			StubHttpMessageHandler handler = new ( _ => Responses.Json( "{}" ) );
			StubHttpClientFactory factory = new ( handler );
			Pug.HttpApiClient.HttpApiClient client = new ( new Uri( "https://h/v1" ), factory );
			MediaTypeWithQualityHeaderValue mediaType = new ( "application/json" );

			await client.GetAsync( "widgets", mediaType, null, null );

			RecordedRequest request = handler.LastRequest;
			Assert.Equal( HttpMethod.Get, request.Method );
			Assert.Equal( new Uri( "https://h/v1/widgets" ), request.RequestUri );
			Assert.Contains( "application/json", request.Accept );
		}

		[Fact]
		public async Task PostAsync_SendsPostWithBodyAndAcceptHeader()
		{
			StubHttpMessageHandler handler = new ( _ => Responses.Json( "{}" ) );
			StubHttpClientFactory factory = new ( handler );
			Pug.HttpApiClient.HttpApiClient client = new ( new Uri( "https://h/v1" ), factory );
			MediaTypeWithQualityHeaderValue mediaType = new ( "application/json" );
			StringContent content = new ( "{\"name\":\"widget\"}", Encoding.UTF8, "application/json" );

			await client.PostAsync( "widgets", content, mediaType );

			RecordedRequest request = handler.LastRequest;
			Assert.Equal( HttpMethod.Post, request.Method );
			Assert.Equal( new Uri( "https://h/v1/widgets" ), request.RequestUri );
			Assert.Contains( "application/json", request.Accept );
			Assert.Equal( "{\"name\":\"widget\"}", request.Body );
		}

		[Fact]
		public async Task PutAsync_SendsPutWithBodyAndAcceptHeader()
		{
			StubHttpMessageHandler handler = new ( _ => Responses.Json( "{}" ) );
			StubHttpClientFactory factory = new ( handler );
			Pug.HttpApiClient.HttpApiClient client = new ( new Uri( "https://h/v1" ), factory );
			MediaTypeWithQualityHeaderValue mediaType = new ( "application/json" );
			StringContent content = new ( "{\"name\":\"widget-updated\"}", Encoding.UTF8, "application/json" );

			await client.PutAsync( "widgets/1", content, mediaType, null, null );

			RecordedRequest request = handler.LastRequest;
			Assert.Equal( HttpMethod.Put, request.Method );
			Assert.Equal( new Uri( "https://h/v1/widgets/1" ), request.RequestUri );
			Assert.Contains( "application/json", request.Accept );
			Assert.Equal( "{\"name\":\"widget-updated\"}", request.Body );
		}

		[Fact]
		public async Task DeleteAsync_SendsDelete()
		{
			StubHttpMessageHandler handler = new ( _ => Responses.Json( "{}" ) );
			StubHttpClientFactory factory = new ( handler );
			Pug.HttpApiClient.HttpApiClient client = new ( new Uri( "https://h/v1" ), factory );

			await client.DeleteAsync( "widgets/1" );

			RecordedRequest request = handler.LastRequest;
			Assert.Equal( HttpMethod.Delete, request.Method );
			Assert.Equal( new Uri( "https://h/v1/widgets/1" ), request.RequestUri );
		}

		[Fact]
		public async Task PatchAsync_SendsPatchMethodWithBody()
		{
			StubHttpMessageHandler handler = new ( _ => Responses.Json( "{}" ) );
			StubHttpClientFactory factory = new ( handler );
			Pug.HttpApiClient.HttpApiClient client = new ( new Uri( "https://h/v1" ), factory );
			StringContent content = new ( "{\"name\":\"widget-patched\"}", Encoding.UTF8, "application/json" );

			await client.PatchAsync( "widgets/1", content, null, null, null );

			RecordedRequest request = handler.LastRequest;
			Assert.Equal( "PATCH", request.Method.Method );
			Assert.Equal( new Uri( "https://h/v1/widgets/1" ), request.RequestUri );
			Assert.Equal( "{\"name\":\"widget-patched\"}", request.Body );
		}

		[Fact]
		public void Ctor_NullUriBaseUrl_ThrowsArgumentNullException()
		{
			StubHttpClientFactory factory = new ( new StubHttpMessageHandler( _ => Responses.Json( "{}" ) ) );

			ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
				() => new Pug.HttpApiClient.HttpApiClient( (Uri)null, factory ) );

			Assert.Equal( "baseUrl", exception.ParamName );
		}

		[Fact]
		public void Ctor_NullHttpClientFactory_ThrowsArgumentNullException()
		{
			ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
				() => new Pug.HttpApiClient.HttpApiClient( new Uri( "https://h/v1" ), (IHttpClientFactory)null ) );

			Assert.Equal( "httpClientFactory", exception.ParamName );
		}

		[Fact]
		public void Ctor_StringBaseUrlOverload_BuildsSameClientAsUriOverload()
		{
			StubHttpClientFactory factory = new ( new StubHttpMessageHandler( _ => Responses.Json( "{}" ) ) );

			Pug.HttpApiClient.HttpApiClient viaUri = new ( new Uri( "https://h/v1" ), factory );
			Pug.HttpApiClient.HttpApiClient viaString = new ( "https://h/v1", factory );

			Assert.Equal( viaUri.BaseAddress, viaString.BaseAddress );
			Assert.Equal( viaUri.BasePath, viaString.BasePath );
			Assert.Equal( viaUri.BaseUrl, viaString.BaseUrl );
		}

		[Fact]
		public async Task GetAsync_SuccessResponse_ReturnedAsIsWithContentIntact()
		{
			StubHttpMessageHandler handler = new ( _ => Responses.Json( "{\"id\":42}" ) );
			StubHttpClientFactory factory = new ( handler );
			Pug.HttpApiClient.HttpApiClient client = new ( new Uri( "https://h/v1" ), factory );

			HttpResponseMessage response = await client.GetAsync( "widgets/42", null, null, null );

			Assert.Equal( HttpStatusCode.OK, response.StatusCode );
			Assert.Equal( "{\"id\":42}", await response.Content.ReadAsStringAsync() );
		}

		[Fact]
		public async Task SendAsync_TwoSequentialRequests_BothSucceedAndAreRecorded()
		{
			StubHttpMessageHandler handler = new ( _ => Responses.Json( "{}" ) );
			StubHttpClientFactory factory = new ( handler );
			Pug.HttpApiClient.HttpApiClient client = new ( new Uri( "https://h/v1" ), factory );

			HttpResponseMessage first = await client.GetAsync( "widgets/1", null, null, null );
			HttpResponseMessage second = await client.GetAsync( "widgets/2", null, null, null );

			Assert.Equal( HttpStatusCode.OK, first.StatusCode );
			Assert.Equal( HttpStatusCode.OK, second.StatusCode );
			Assert.Equal( 2, handler.RequestCount );
			Assert.Equal( 2, factory.CreateClientCount );
		}
	}
}
