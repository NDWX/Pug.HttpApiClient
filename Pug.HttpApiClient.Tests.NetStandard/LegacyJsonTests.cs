using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Pug.HttpApiClient.Json;
using Pug.HttpApiClient.Tests.Infrastructure;
using Xunit;

namespace Pug.HttpApiClient.Tests.NetStandard
{
	/// <summary>
	/// Mutating <see cref="IHttpApiClientJsonExtensions.SetJsonSerializerSettings"/> changes process-wide state
	/// shared by every JSON call in this assembly, so the tests that touch it must not interleave with each other.
	/// </summary>
	[CollectionDefinition( "NetStandard JSON default serializer settings", DisableParallelization = true )]
	public class JsonDefaultSerializerSettingsCollection
	{
	}

	public sealed class Widget
	{
		public string Name { get; set; }
		public int Count { get; set; }
	}

	/// <summary>
	/// Newtonsoft.Json round trip and the NETCOREAPP2_1||NETSTANDARD-only settings surface of
	/// <see cref="IHttpApiClientJsonExtensions"/>. Only meaningful when this project is genuinely bound to the
	/// netstandard2.0 compilation - see <see cref="TargetFrameworkProbe"/>.
	/// </summary>
	[Trait( "Category", "NetStandard" )]
	[Collection( "NetStandard JSON default serializer settings" )]
	public class LegacyJsonTests
	{
		private static HttpApiClient CreateClient( StubHttpMessageHandler handler ) =>
			new ( new Uri( "https://api.example.com/v1" ), new StubHttpClientFactory( handler ) );

		[Fact]
		public async Task GetAsync_DeserializesResponseBody_ThroughNewtonsoftJsonTextReader()
		{
			StubHttpMessageHandler handler = new ( _ => Responses.Json( "{\"Name\":\"widget-a\",\"Count\":3}" ) );
			HttpApiClient client = CreateClient( handler );

			Widget widget = await client.GetAsync<Widget>( "widgets/1" );

			Assert.Equal( "widget-a", widget.Name );
			Assert.Equal( 3, widget.Count );
		}

		[Fact]
		public async Task PostAsync_SerializesRequestBody_ThroughJsonConvert_AndDeserializesResult()
		{
			StubHttpMessageHandler handler = new ( _ => Responses.Json( "{\"Name\":\"widget-b\",\"Count\":7}" ) );
			HttpApiClient client = CreateClient( handler );

			Widget content = new () { Name = "widget-b", Count = 7 };

			Widget result = await client.PostAsync<Widget, Widget>( "widgets", content );

			Assert.Equal( "{\"Name\":\"widget-b\",\"Count\":7}", handler.LastRequest.Body );
			Assert.Equal( "widget-b", result.Name );
			Assert.Equal( 7, result.Count );
		}

		[Fact]
		public async Task PutAsync_SerializesRequestBody_ThroughJsonConvert_AndDeserializesResult()
		{
			StubHttpMessageHandler handler = new ( _ => Responses.Json( "{\"Name\":\"widget-c\",\"Count\":8}" ) );
			HttpApiClient client = CreateClient( handler );

			Widget content = new () { Name = "widget-c", Count = 8 };

			Widget result = await client.PutAsync<Widget, Widget>( "widgets/1", content );

			Assert.Equal( "{\"Name\":\"widget-c\",\"Count\":8}", handler.LastRequest.Body );
			Assert.Equal( "widget-c", result.Name );
			Assert.Equal( 8, result.Count );
		}

		[Fact]
		public async Task PatchAsync_SerializesRequestBody_ThroughJsonConvert_AndDeserializesResult()
		{
			StubHttpMessageHandler handler = new ( _ => Responses.Json( "{\"Name\":\"widget-d\",\"Count\":9}" ) );
			HttpApiClient client = CreateClient( handler );

			Widget content = new () { Name = "widget-d", Count = 9 };

			Widget result = await client.PatchAsync<Widget, Widget>( "widgets/1", content );

			Assert.Equal( "{\"Name\":\"widget-d\",\"Count\":9}", handler.LastRequest.Body );
			Assert.Equal( "widget-d", result.Name );
			Assert.Equal( 9, result.Count );
		}

		[Fact]
		public void SetJsonSerializerSettings_IsAPlainStaticMethod_NotAnExtensionMethod()
		{
			// The modern build's equivalent, SetJsonSerializerOptions, is declared `this IHttpApiClient ...` -
			// an extension method. This one is a bare static method: no receiver, called as
			// IHttpApiClientJsonExtensions.SetJsonSerializerSettings(...), never as an instance member.
			MethodInfo method = typeof(IHttpApiClientJsonExtensions).GetMethod(
					nameof(IHttpApiClientJsonExtensions.SetJsonSerializerSettings),
					BindingFlags.Public | BindingFlags.Static
				);

			Assert.NotNull( method );
			Assert.Null( method.GetCustomAttribute<ExtensionAttribute>() );
		}

		[Fact]
		public async Task SetJsonSerializerSettings_ChangesDefaultSettings_ForSubsequentJsonCallsWithNoExplicitSettings()
		{
			JsonSerializerSettings original = IHttpApiClientJsonExtensions.DefaultJsonSerializerSettings;

			try
			{
				IHttpApiClientJsonExtensions.SetJsonSerializerSettings(
						new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }
					);

				StubHttpMessageHandler handler = new ( _ => Responses.Json( "{\"name\":\"widget-e\",\"count\":11}" ) );
				HttpApiClient client = CreateClient( handler );

				Widget content = new () { Name = "widget-e", Count = 11 };

				// No jsonSerializerSettings argument: this must fall back to DefaultJsonSerializerSettings.
				Widget result = await client.PostAsync<Widget, Widget>( "widgets", content );

				Assert.Equal( "{\"name\":\"widget-e\",\"count\":11}", handler.LastRequest.Body );
				Assert.Equal( "widget-e", result.Name );
				Assert.Equal( 11, result.Count );
			}
			finally
			{
				IHttpApiClientJsonExtensions.SetJsonSerializerSettings( original );
			}
		}

		[Fact]
		public async Task PostAsync_WithCallerSuppliedSettings_UsesCamelCasePropertyNamesInRequestBody()
		{
			StubHttpMessageHandler handler = new ( _ => Responses.Json( "{\"name\":\"widget-f\",\"count\":12}" ) );
			HttpApiClient client = CreateClient( handler );

			JsonSerializerSettings settings = new () { ContractResolver = new CamelCasePropertyNamesContractResolver() };
			Widget content = new () { Name = "widget-f", Count = 12 };

			Widget result = await client.PostAsync<Widget, Widget>( "widgets", content, jsonSerializerSettings: settings );

			Assert.Equal( "{\"name\":\"widget-f\",\"count\":12}", handler.LastRequest.Body );
			Assert.Equal( "widget-f", result.Name );
			Assert.Equal( 12, result.Count );
		}

		[Fact]
		public async Task PutAsync_WithCallerSuppliedSettings_UsesCamelCasePropertyNamesInRequestBody()
		{
			// Proof of the fix: PutAsync<TContent,TResult> used to drop the caller-supplied settings entirely and
			// fall back to DefaultJsonSerializerSettings, which would have produced "Name"/"Count" (PascalCase) here.
			StubHttpMessageHandler handler = new ( _ => Responses.Json( "{\"name\":\"widget-g\",\"count\":13}" ) );
			HttpApiClient client = CreateClient( handler );

			JsonSerializerSettings settings = new () { ContractResolver = new CamelCasePropertyNamesContractResolver() };
			Widget content = new () { Name = "widget-g", Count = 13 };

			Widget result = await client.PutAsync<Widget, Widget>( "widgets/1", content, jsonSerializerSettings: settings );

			Assert.Equal( "{\"name\":\"widget-g\",\"count\":13}", handler.LastRequest.Body );
			Assert.Equal( "widget-g", result.Name );
			Assert.Equal( 13, result.Count );
		}

		[Fact]
		public async Task PatchAsync_WithCallerSuppliedSettings_UsesCamelCasePropertyNamesInRequestBody()
		{
			// Proof of the fix: PatchAsync<TContent,TResult> used to drop the caller-supplied settings entirely and
			// fall back to DefaultJsonSerializerSettings, which would have produced "Name"/"Count" (PascalCase) here.
			StubHttpMessageHandler handler = new ( _ => Responses.Json( "{\"name\":\"widget-h\",\"count\":14}" ) );
			HttpApiClient client = CreateClient( handler );

			JsonSerializerSettings settings = new () { ContractResolver = new CamelCasePropertyNamesContractResolver() };
			Widget content = new () { Name = "widget-h", Count = 14 };

			Widget result = await client.PatchAsync<Widget, Widget>( "widgets/1", content, jsonSerializerSettings: settings );

			Assert.Equal( "{\"name\":\"widget-h\",\"count\":14}", handler.LastRequest.Body );
			Assert.Equal( "widget-h", result.Name );
			Assert.Equal( 14, result.Count );
		}

		[Fact]
		public async Task GetAsync_SendsAcceptHeader_AsHardcodedApplicationJsonString()
		{
			StubHttpMessageHandler handler = new ( _ => Responses.Json( "{}" ) );
			HttpApiClient client = CreateClient( handler );

			await client.GetAsync<Widget>( "widgets/1" );

			Assert.Equal( "application/json", handler.LastRequest.Accept[0] );
		}

		[Fact]
		public async Task PostAsync_SendsContentTypeAndAcceptHeaders_AsHardcodedApplicationJsonString()
		{
			StubHttpMessageHandler handler = new ( _ => Responses.Json( "{}" ) );
			HttpApiClient client = CreateClient( handler );

			await client.PostAsync<Widget, Widget>( "widgets", new Widget { Name = "widget-i", Count = 1 } );

			Assert.Equal( "application/json", handler.LastRequest.Accept[0] );
			Assert.Equal( "application/json", handler.LastRequest.ContentType );
		}
	}
}
