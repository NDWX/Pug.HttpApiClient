using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pug.HttpApiClient.Tests.Infrastructure;
using Xunit;

namespace Pug.HttpApiClient.Tests.Client
{
	public class DecoratorTests
	{
		private sealed class DelegateClientDecorator : IHttpClientDecorator
		{
			private readonly Action<DecorationContext> _decorate;

			public DelegateClientDecorator( Action<DecorationContext> decorate )
			{
				_decorate = decorate;
			}

			public void Decorate( DecorationContext context ) => _decorate( context );

			public Task DecorateAsync( DecorationContext context )
			{
				_decorate( context );
				return Task.CompletedTask;
			}
		}

		private sealed class DelegateMessageDecorator : IHttpRequestMessageDecorator
		{
			private readonly Action<MessageDecorationContext> _decorate;

			public DelegateMessageDecorator( Action<MessageDecorationContext> decorate )
			{
				_decorate = decorate;
			}

			public void Decorate( MessageDecorationContext context ) => _decorate( context );

			public Task DecorateAsync( MessageDecorationContext context )
			{
				_decorate( context );
				return Task.CompletedTask;
			}
		}

		[Fact]
		public async Task ClientDecorator_MutatesDefaultRequestHeaders_HeaderReachesTransport()
		{
			StubHttpMessageHandler handler = new ( _ => Responses.Json( "{}" ) );
			StubHttpClientFactory factory = new ( handler );
			DelegateClientDecorator decorator = new ( context => context.RequestHeaders.Add( "X-Client-Decorator", "yes" ) );
			Pug.HttpApiClient.HttpApiClient client = new (
				new Uri( "https://h/v1" ), factory, new IHttpClientDecorator[] { decorator } );

			await client.GetAsync( "widgets", null, null, null );

			Assert.Equal( "yes", handler.LastRequest.GetHeader( "X-Client-Decorator" ) );
		}

		[Fact]
		public async Task MessageDecorator_MutatesPerRequestHeaders_HeaderReachesTransport()
		{
			StubHttpMessageHandler handler = new ( _ => Responses.Json( "{}" ) );
			StubHttpClientFactory factory = new ( handler );
			DelegateMessageDecorator decorator = new ( context => context.RequestHeaders.Add( "X-Message-Decorator", "yes" ) );
			Pug.HttpApiClient.HttpApiClient client = new (
				new Uri( "https://h/v1" ), factory, null, new IHttpRequestMessageDecorator[] { decorator } );

			await client.GetAsync( "widgets", null, null, null );

			Assert.Equal( "yes", handler.LastRequest.GetHeader( "X-Message-Decorator" ) );
		}

		[Fact]
		public async Task AuthorizationHeaderDecorator_Decorate_SetsSchemeAndParameter()
		{
			StubHttpMessageHandler handler = new ( _ => Responses.Json( "{}" ) );
			StubHttpClientFactory factory = new ( handler );
			AuthorizationHeaderDecorator decorator = new ( "Bearer", "tok123" );
			Pug.HttpApiClient.HttpApiClient client = new (
				new Uri( "https://h/v1" ), factory, null, new IHttpRequestMessageDecorator[] { decorator } );

			await client.GetAsync( "widgets", null, null, null );

			Assert.Equal( "Bearer", handler.LastRequest.AuthorizationScheme );
			Assert.Equal( "tok123", handler.LastRequest.AuthorizationParameter );
		}

		[Theory]
		[InlineData( null, "value" )]
		[InlineData( "", "value" )]
		[InlineData( "   ", "value" )]
		public void AuthorizationHeaderDecorator_Ctor_NullOrWhitespaceType_ThrowsArgumentException( string type, string value )
		{
			ArgumentException exception = Assert.Throws<ArgumentException>( () => new AuthorizationHeaderDecorator( type, value ) );

			Assert.Equal( "type", exception.ParamName );
		}

		[Theory]
		[InlineData( "Bearer", null )]
		[InlineData( "Bearer", "" )]
		[InlineData( "Bearer", "   " )]
		public void AuthorizationHeaderDecorator_Ctor_NullOrWhitespaceValue_ThrowsArgumentException( string type, string value )
		{
			ArgumentException exception = Assert.Throws<ArgumentException>( () => new AuthorizationHeaderDecorator( type, value ) );

			Assert.Equal( "value", exception.ParamName );
		}

		[Fact]
		public async Task ExplicitHeader_OverwritesHeaderSetByMessageDecorator()
		{
			StubHttpMessageHandler handler = new ( _ => Responses.Json( "{}" ) );
			StubHttpClientFactory factory = new ( handler );
			DelegateMessageDecorator decorator = new ( context => context.RequestHeaders.Add( "X-Test", "fromDecorator" ) );
			Pug.HttpApiClient.HttpApiClient client = new (
				new Uri( "https://h/v1" ), factory, null, new IHttpRequestMessageDecorator[] { decorator } );

			await client.GetAsync( "widgets", null, new Dictionary<string, string> { ["X-Test"] = "fromCaller" }, null );

			Assert.Equal( "fromCaller", handler.LastRequest.GetHeader( "X-Test" ) );
		}

		[Fact]
		public async Task MessageDecorator_ContributingUrlQueries_UnionedWithCallerQueries()
		{
			StubHttpMessageHandler handler = new ( _ => Responses.Json( "{}" ) );
			StubHttpClientFactory factory = new ( handler );
			DelegateMessageDecorator decorator = new (
				context => context.UrlQueries = new Dictionary<string, string> { ["decoratorKey"] = "decoratorVal" } );
			Pug.HttpApiClient.HttpApiClient client = new (
				new Uri( "https://h/v1" ), factory, null, new IHttpRequestMessageDecorator[] { decorator } );

			await client.GetAsync( "widgets", null, null, new Dictionary<string, string> { ["callerKey"] = "callerVal" } );

			string query = handler.LastRequest.Query;
			Assert.Contains( "callerKey=callerVal", query );
			Assert.Contains( "decoratorKey=decoratorVal", query );
		}

		[Fact]
		public async Task MultipleQueryContributingDecorators_OnlyLastDecoratorQueriesSurvive()
		{
			// KNOWN BUG, documented not fixed: HttpApiClient.CreateRequestMessageAsync reassigns the
			// local `uriQueries` variable inside the decorator loop instead of accumulating into it,
			// and its null-check re-tests the ORIGINAL (unmodified) `queries` parameter on every
			// iteration rather than the running `uriQueries` value. With no caller-supplied queries,
			// each decorator's UrlQueries therefore REPLACES the previous decorator's contribution
			// instead of being unioned with it, so only the last decorator's queries reach the wire.
			// See HttpApiClient.cs, CreateRequestMessageAsync (the `uriQueries = queries is null ? ... `
			// line inside the `foreach( IHttpRequestMessageDecorator messageDecorator ...)` loop).
			StubHttpMessageHandler handler = new ( _ => Responses.Json( "{}" ) );
			StubHttpClientFactory factory = new ( handler );
			DelegateMessageDecorator first = new ( context => context.UrlQueries = new Dictionary<string, string> { ["a"] = "1" } );
			DelegateMessageDecorator second = new ( context => context.UrlQueries = new Dictionary<string, string> { ["b"] = "2" } );
			Pug.HttpApiClient.HttpApiClient client = new (
				new Uri( "https://h/v1" ), factory, null, new IHttpRequestMessageDecorator[] { first, second } );

			await client.GetAsync( "widgets", null, null, null );

			Assert.Equal( "b=2", handler.LastRequest.Query );
		}
	}
}
