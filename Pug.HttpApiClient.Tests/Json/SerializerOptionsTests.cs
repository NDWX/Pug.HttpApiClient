using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Pug.HttpApiClient.Json;
using Pug.HttpApiClient.Tests.Infrastructure;
using Xunit;

namespace Pug.HttpApiClient.Tests.Json
{
	/// <summary>
	/// Serializer-options plumbing. Several of these mutate the process-wide default held in a static
	/// field, so the whole class runs in its own non-parallel collection and every mutating test
	/// restores the original in a <c>finally</c>.
	/// </summary>
	[Collection( SerializerOptionsCollection.Name )]
	public class SerializerOptionsTests
	{
		private const string BaseUrl = "https://api.example.com/v1";

		private static (HttpApiClient Client, StubHttpMessageHandler Handler) CreateClient( string responseBody = "{}" )
		{
			StubHttpMessageHandler handler = new ( _ => Responses.Create( HttpStatusCode.OK, responseBody ) );

			return ( new HttpApiClient( BaseUrl, new StubHttpClientFactory( handler ) ), handler );
		}

		private static JsonSerializerOptions CamelCase() =>
			new () { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

		// Fix #2: CreateHttpContent used to do `defaultOptions ?? callerOptions`. The default is never
		// null, so caller-supplied options were ALWAYS discarded. Fix #3: Put and Patch additionally
		// never forwarded the options to CreateHttpContent at all, while Post did.
		// Each of these three asserts the caller's naming policy actually shaped the request body.

		[Fact]
		public async Task PostAsync_WithCallerSuppliedOptions_ShapesRequestBody()
		{
			( HttpApiClient client, StubHttpMessageHandler handler ) = CreateClient( @"{""Reference"":""r"",""LineCount"":1}" );

			await client.PostAsync<Widget, WidgetOrder>(
					"orders", new Widget { Name = "bolt", Quantity = 7 },
					jsonSerializerOptions: CamelCase() );

			Assert.Equal( @"{""name"":""bolt"",""quantity"":7}", handler.LastRequest.Body );
		}

		[Fact]
		public async Task PutAsync_WithCallerSuppliedOptions_ShapesRequestBody()
		{
			( HttpApiClient client, StubHttpMessageHandler handler ) = CreateClient( @"{""Reference"":""r"",""LineCount"":1}" );

			await client.PutAsync<Widget, WidgetOrder>(
					"orders/1", new Widget { Name = "bolt", Quantity = 7 },
					jsonSerializerOptions: CamelCase() );

			Assert.Equal( @"{""name"":""bolt"",""quantity"":7}", handler.LastRequest.Body );
		}

		[Fact]
		public async Task PatchAsync_WithCallerSuppliedOptions_ShapesRequestBody()
		{
			( HttpApiClient client, StubHttpMessageHandler handler ) = CreateClient( @"{""Reference"":""r"",""LineCount"":1}" );

			await client.PatchAsync<Widget, WidgetOrder>(
					"orders/1", new Widget { Name = "bolt", Quantity = 7 },
					jsonSerializerOptions: CamelCase() );

			Assert.Equal( @"{""name"":""bolt"",""quantity"":7}", handler.LastRequest.Body );
		}

		[Fact]
		public async Task GetAsync_CallerSuppliedOptions_TakePrecedenceOverStaticDefault()
		{
			// The static default has PropertyNameCaseInsensitive = true, so lower-cased JSON would bind.
			// Passing options that turn it OFF must win: Name stays null. If the caller's options were
			// ignored in favour of the default, "washer" would bind and this fails.
			( HttpApiClient client, _ ) = CreateClient( @"{""name"":""washer"",""quantity"":3}" );

			Widget widget = await client.GetAsync<Widget>(
					"widgets/1",
					jsonSerializerOptions: new JsonSerializerOptions { PropertyNameCaseInsensitive = false } );

			Assert.Null( widget.Name );
			Assert.Equal( 0, widget.Quantity );
		}

		[Fact]
		public async Task SetJsonSerializerOptions_CalledOnOneClient_AffectsEveryClientGlobally()
		{
			// SetJsonSerializerOptions is shaped as an extension method on IHttpApiClient, which implies
			// per-client scope. It actually assigns a private STATIC field, so the effect is process-wide.
			// This test pins that discrepancy rather than endorsing it.
			JsonSerializerOptions original = IHttpApiClientJsonExtensions.DefaultJsonSerializerOptions;

			try
			{
				( HttpApiClient configuredClient, _ ) = CreateClient();
				( HttpApiClient unrelatedClient, StubHttpMessageHandler unrelatedHandler ) =
					CreateClient( @"{""Reference"":""r"",""LineCount"":1}" );

				configuredClient.SetJsonSerializerOptions( CamelCase() );

				// the change was made through configuredClient, but unrelatedClient serializes camelCase too
				await unrelatedClient.PostAsync<Widget, WidgetOrder>( "orders", new Widget { Name = "bolt", Quantity = 7 } );

				Assert.Equal( @"{""name"":""bolt"",""quantity"":7}", unrelatedHandler.LastRequest.Body );
				Assert.NotSame( original, IHttpApiClientJsonExtensions.DefaultJsonSerializerOptions );
			}
			finally
			{
				// restore so this cannot leak into any other test
				( HttpApiClient restoreClient, _ ) = CreateClient();
				restoreClient.SetJsonSerializerOptions( original );
			}
		}
	}

	[CollectionDefinition( Name, DisableParallelization = true )]
	public sealed class SerializerOptionsCollection
	{
		public const string Name = "serializer-options";
	}
}
