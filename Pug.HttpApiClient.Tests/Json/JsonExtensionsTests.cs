using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Pug.HttpApiClient.Json;
using Pug.HttpApiClient.Tests.Infrastructure;
using Xunit;

namespace Pug.HttpApiClient.Tests.Json
{
	public class JsonExtensionsTests
	{
		private const string BaseUrl = "https://api.example.com/v1";

		private static (HttpApiClient Client, StubHttpMessageHandler Handler) CreateClient(
			HttpStatusCode statusCode = HttpStatusCode.OK, string responseBody = "{}" )
		{
			StubHttpMessageHandler handler = new ( _ => Responses.Create( statusCode, responseBody ) );

			return ( new HttpApiClient( BaseUrl, new StubHttpClientFactory( handler ) ), handler );
		}

		[Fact]
		public async Task GetAsync_DeserializesResponseBodyIntoRequestedType()
		{
			( HttpApiClient client, _ ) = CreateClient( responseBody: @"{""Name"":""bolt"",""Quantity"":7}" );

			Widget widget = await client.GetAsync<Widget>( "widgets/1" );

			Assert.Equal( "bolt", widget.Name );
			Assert.Equal( 7, widget.Quantity );
		}

		[Fact]
		public async Task GetAsync_SendsApplicationJsonAcceptHeader()
		{
			( HttpApiClient client, StubHttpMessageHandler handler ) = CreateClient( responseBody: "{}" );

			await client.GetAsync<Widget>( "widgets/1" );

			Assert.Contains( "application/json", handler.LastRequest.Accept );
		}

		[Fact]
		public async Task GetAsync_PassesHeadersAndQueriesToTransport()
		{
			( HttpApiClient client, StubHttpMessageHandler handler ) = CreateClient( responseBody: "{}" );

			await client.GetAsync<Widget>(
					"widgets",
					new Dictionary<string, string> { ["X-Tenant"] = "acme" },
					new Dictionary<string, string> { ["page"] = "2" } );

			Assert.Equal( "acme", handler.LastRequest.GetHeader( "X-Tenant" ) );
			Assert.Equal( "page=2", handler.LastRequest.Query );
		}

		[Fact]
		public async Task GetAsync_DefaultOptions_BindPropertiesCaseInsensitively()
		{
			// the static default sets PropertyNameCaseInsensitive = true, so a lower-cased
			// JSON property still binds to the Pascal-cased CLR property
			( HttpApiClient client, _ ) = CreateClient( responseBody: @"{""name"":""washer"",""quantity"":3}" );

			Widget widget = await client.GetAsync<Widget>( "widgets/1" );

			Assert.Equal( "washer", widget.Name );
			Assert.Equal( 3, widget.Quantity );
		}

		[Fact]
		public async Task GetAsync_UnmappedNonSuccessStatus_ThrowsHttpApiRequestException()
		{
			// 429 is deliberate: SendAsync's status switch does not map it, so the response reaches
			// CheckAndDeserializeAsync and it is that method's own guard which throws. Using 400 or 500
			// here would be intercepted earlier and would test the wrong code path.
			( HttpApiClient client, _ ) = CreateClient( HttpStatusCode.TooManyRequests, @"{""error"":""slow down""}" );

			HttpApiRequestException exception =
				await Assert.ThrowsAsync<HttpApiRequestException>( () => client.GetAsync<Widget>( "widgets/1" ) );

			Assert.Equal( HttpStatusCode.TooManyRequests, exception.ResponseStatusCode );
			Assert.Contains( "slow down", exception.ResponseMessage );
		}

		[Theory]
		[InlineData( "POST" )]
		[InlineData( "PUT" )]
		[InlineData( "PATCH" )]
		public async Task ContentVerbs_SerializeBodyAndSetJsonContentType( string method )
		{
			( HttpApiClient client, StubHttpMessageHandler handler ) = CreateClient( responseBody: "{}" );

			Widget widget = new () { Name = "bolt", Quantity = 7 };

			HttpResponseMessage response = method switch
			{
				"POST" => await client.PostAsync<Widget>( "widgets", widget ),
				"PUT" => await client.PutAsync<Widget>( "widgets/1", widget ),
				_ => await client.PatchAsync<Widget>( "widgets/1", widget )
			};

			RecordedRequest request = handler.LastRequest;

			Assert.Equal( method, request.Method.Method );
			Assert.Equal( @"{""Name"":""bolt"",""Quantity"":7}", request.Body );
			Assert.StartsWith( "application/json", request.ContentType );

			// the single-type-parameter overloads hand back the raw response rather than deserializing
			Assert.Equal( HttpStatusCode.OK, response.StatusCode );
		}

		[Fact]
		public async Task PostAsync_WithResultType_RoundTripsThroughJson()
		{
			( HttpApiClient client, StubHttpMessageHandler handler ) =
				CreateClient( responseBody: @"{""Reference"":""ORD-1"",""LineCount"":2}" );

			WidgetOrder order = await client.PostAsync<Widget, WidgetOrder>( "orders", new Widget { Name = "bolt", Quantity = 7 } );

			Assert.Equal( @"{""Name"":""bolt"",""Quantity"":7}", handler.LastRequest.Body );
			Assert.Equal( "ORD-1", order.Reference );
			Assert.Equal( 2, order.LineCount );
		}

		[Fact]
		public async Task PutAsync_WithResultType_RoundTripsThroughJson()
		{
			( HttpApiClient client, StubHttpMessageHandler handler ) =
				CreateClient( responseBody: @"{""Reference"":""ORD-2"",""LineCount"":5}" );

			WidgetOrder order = await client.PutAsync<Widget, WidgetOrder>( "orders/2", new Widget { Name = "nut", Quantity = 5 } );

			Assert.Equal( @"{""Name"":""nut"",""Quantity"":5}", handler.LastRequest.Body );
			Assert.Equal( "ORD-2", order.Reference );
			Assert.Equal( 5, order.LineCount );
		}

		[Fact]
		public async Task PatchAsync_WithResultType_RoundTripsThroughJson()
		{
			( HttpApiClient client, StubHttpMessageHandler handler ) =
				CreateClient( responseBody: @"{""Reference"":""ORD-3"",""LineCount"":1}" );

			WidgetOrder order = await client.PatchAsync<Widget, WidgetOrder>( "orders/3", new Widget { Name = "screw", Quantity = 1 } );

			Assert.Equal( @"{""Name"":""screw"",""Quantity"":1}", handler.LastRequest.Body );
			Assert.Equal( "ORD-3", order.Reference );
			Assert.Equal( 1, order.LineCount );
		}
	}
}
