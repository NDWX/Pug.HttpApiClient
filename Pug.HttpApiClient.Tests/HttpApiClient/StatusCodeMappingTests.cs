using System;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading.Tasks;
using Pug;
using Pug.HttpApiClient.Tests.Infrastructure;
using Xunit;

namespace Pug.HttpApiClient.Tests.Client
{
	public class StatusCodeMappingTests
	{
		private static Pug.HttpApiClient.HttpApiClient CreateClient( StubHttpMessageHandler handler )
		{
			StubHttpClientFactory factory = new ( handler );
			return new ( new Uri( "https://h/v1" ), factory );
		}

		[Fact]
		public async Task Get_403Forbidden_ThrowsAuthorizationExceptionWithReasonPhraseAsMessage()
		{
			StubHttpMessageHandler handler = new ( _ => Responses.Status( HttpStatusCode.Forbidden, "Custom Forbidden Reason" ) );
			Pug.HttpApiClient.HttpApiClient client = CreateClient( handler );

			Exception exception = await Record.ExceptionAsync( () => client.GetAsync( "widgets", null, null, null ) );

			// Pug.AuthorizationException (from the Pug.Core package) is not HttpApiRequestException-derived:
			// it exposes no ResponseStatusCode/ResponseStatusReason/ResponseMessage, only Message.
			AuthorizationException authorizationException = Assert.IsType<AuthorizationException>( exception );
			Assert.Equal( "Custom Forbidden Reason", authorizationException.Message );
		}

		[Fact]
		public async Task Get_401Unauthorized_ThrowsAuthenticationException()
		{
			StubHttpMessageHandler handler = new ( _ => Responses.Status( HttpStatusCode.Unauthorized ) );
			Pug.HttpApiClient.HttpApiClient client = CreateClient( handler );

			Exception exception = await Record.ExceptionAsync( () => client.GetAsync( "widgets", null, null, null ) );

			// System.Security.Authentication.AuthenticationException() is thrown with its parameterless
			// ctor - no response details are carried over.
			Assert.IsType<AuthenticationException>( exception );
		}

		[Fact]
		public async Task Get_404NotFound_ThrowsUnknownResourceExceptionWithResponseDetails()
		{
			StubHttpMessageHandler handler = new (
				_ => Responses.Create( HttpStatusCode.NotFound, "widget not found", "text/plain", "Custom Not Found Reason" ) );
			Pug.HttpApiClient.HttpApiClient client = CreateClient( handler );

			Exception exception = await Record.ExceptionAsync( () => client.GetAsync( "widgets/1", null, null, null ) );

			UnknownResourceException unknownResourceException = Assert.IsType<UnknownResourceException>( exception );
			Assert.Equal( HttpStatusCode.NotFound, unknownResourceException.ResponseStatusCode );
			Assert.Equal( "Custom Not Found Reason", unknownResourceException.ResponseStatusReason );
			Assert.Equal( "widget not found", unknownResourceException.ResponseMessage );
		}

		[Fact]
		public async Task Get_410Gone_ThrowsUnknownResourceExceptionWithResponseDetails()
		{
			StubHttpMessageHandler handler = new (
				_ => Responses.Create( HttpStatusCode.Gone, "widget gone", "text/plain", "Custom Gone Reason" ) );
			Pug.HttpApiClient.HttpApiClient client = CreateClient( handler );

			Exception exception = await Record.ExceptionAsync( () => client.GetAsync( "widgets/1", null, null, null ) );

			UnknownResourceException unknownResourceException = Assert.IsType<UnknownResourceException>( exception );
			Assert.Equal( HttpStatusCode.Gone, unknownResourceException.ResponseStatusCode );
			Assert.Equal( "Custom Gone Reason", unknownResourceException.ResponseStatusReason );
			Assert.Equal( "widget gone", unknownResourceException.ResponseMessage );
		}

		[Fact]
		public async Task Get_400BadRequest_ThrowsHttpApiRequestExceptionWithResponseDetails()
		{
			StubHttpMessageHandler handler = new (
				_ => Responses.Create( HttpStatusCode.BadRequest, "malformed request", "text/plain", "Custom Bad Request Reason" ) );
			Pug.HttpApiClient.HttpApiClient client = CreateClient( handler );

			Exception exception = await Record.ExceptionAsync( () => client.GetAsync( "widgets", null, null, null ) );

			Pug.HttpApiClient.HttpApiRequestException httpApiRequestException =
				Assert.IsType<Pug.HttpApiClient.HttpApiRequestException>( exception );
			Assert.Equal( HttpStatusCode.BadRequest, httpApiRequestException.ResponseStatusCode );
			Assert.Equal( "Custom Bad Request Reason", httpApiRequestException.ResponseStatusReason );
			Assert.Equal( "malformed request", httpApiRequestException.ResponseMessage );
		}

		[Fact]
		public async Task Get_502BadGateway_ThrowsHttpApiRequestExceptionWithResponseDetails()
		{
			StubHttpMessageHandler handler = new (
				_ => Responses.Create( HttpStatusCode.BadGateway, "upstream failed", "text/plain", "Custom Bad Gateway Reason" ) );
			Pug.HttpApiClient.HttpApiClient client = CreateClient( handler );

			Exception exception = await Record.ExceptionAsync( () => client.GetAsync( "widgets", null, null, null ) );

			Pug.HttpApiClient.HttpApiRequestException httpApiRequestException =
				Assert.IsType<Pug.HttpApiClient.HttpApiRequestException>( exception );
			Assert.Equal( HttpStatusCode.BadGateway, httpApiRequestException.ResponseStatusCode );
			Assert.Equal( "Custom Bad Gateway Reason", httpApiRequestException.ResponseStatusReason );
			Assert.Equal( "upstream failed", httpApiRequestException.ResponseMessage );
		}

		[Fact]
		public async Task Get_405MethodNotAllowed_ThrowsHttpApiRequestExceptionWithInvalidOperationInnerMentioningMethod()
		{
			StubHttpMessageHandler handler = new ( _ => Responses.Status( HttpStatusCode.MethodNotAllowed, "Method Not Allowed" ) );
			Pug.HttpApiClient.HttpApiClient client = CreateClient( handler );

			Exception exception = await Record.ExceptionAsync( () => client.GetAsync( "widgets", null, null, null ) );

			Pug.HttpApiClient.HttpApiRequestException httpApiRequestException =
				Assert.IsType<Pug.HttpApiClient.HttpApiRequestException>( exception );
			InvalidOperationException inner = Assert.IsType<InvalidOperationException>( httpApiRequestException.InnerException );
			Assert.Contains( "GET", inner.Message );
		}

		[Fact]
		public async Task Get_409Conflict_ThrowsHttpApiRequestExceptionWithConflictMessage()
		{
			StubHttpMessageHandler handler = new ( _ => Responses.Status( HttpStatusCode.Conflict, "Conflict" ) );
			Pug.HttpApiClient.HttpApiClient client = CreateClient( handler );

			Exception exception = await Record.ExceptionAsync( () => client.GetAsync( "widgets", null, null, null ) );

			Pug.HttpApiClient.HttpApiRequestException httpApiRequestException =
				Assert.IsType<Pug.HttpApiClient.HttpApiRequestException>( exception );
			Assert.Equal( "Possible authentication/authorization or resource conflict error", httpApiRequestException.Message );
		}

		[Fact]
		public async Task Get_423Locked_ThrowsHttpApiRequestExceptionWithInvalidOperationInner()
		{
			StubHttpMessageHandler handler = new ( _ => Responses.Status( HttpStatusCode.Locked, "Locked" ) );
			Pug.HttpApiClient.HttpApiClient client = CreateClient( handler );

			Exception exception = await Record.ExceptionAsync( () => client.GetAsync( "widgets", null, null, null ) );

			Pug.HttpApiClient.HttpApiRequestException httpApiRequestException =
				Assert.IsType<Pug.HttpApiClient.HttpApiRequestException>( exception );
			Assert.IsType<InvalidOperationException>( httpApiRequestException.InnerException );
		}

		[Fact]
		public async Task Get_501NotImplemented_ThrowsHttpApiRequestExceptionWithNotImplementedInner()
		{
			StubHttpMessageHandler handler = new ( _ => Responses.Status( HttpStatusCode.NotImplemented, "Not Implemented" ) );
			Pug.HttpApiClient.HttpApiClient client = CreateClient( handler );

			Exception exception = await Record.ExceptionAsync( () => client.GetAsync( "widgets", null, null, null ) );

			Pug.HttpApiClient.HttpApiRequestException httpApiRequestException =
				Assert.IsType<Pug.HttpApiClient.HttpApiRequestException>( exception );
			Assert.IsType<NotImplementedException>( httpApiRequestException.InnerException );
		}

		[Fact]
		public async Task Get_500InternalServerError_ThrowsInternalServerErrorExceptionWithResponseDetails()
		{
			StubHttpMessageHandler handler = new (
				_ => Responses.Create( HttpStatusCode.InternalServerError, "server exploded", "text/plain", "Custom Server Error" ) );
			Pug.HttpApiClient.HttpApiClient client = CreateClient( handler );

			Exception exception = await Record.ExceptionAsync( () => client.GetAsync( "widgets", null, null, null ) );

			Pug.HttpApiClient.InternalServerErrorException internalServerErrorException =
				Assert.IsType<Pug.HttpApiClient.InternalServerErrorException>( exception );
			Assert.Equal( HttpStatusCode.InternalServerError, internalServerErrorException.ResponseStatusCode );
			Assert.Equal( "Custom Server Error", internalServerErrorException.ResponseStatusReason );
			Assert.Equal( "server exploded", internalServerErrorException.ResponseMessage );
		}

		[Fact]
		public async Task Get_507InsufficientStorage_ThrowsInternalServerErrorExceptionWithResponseDetails()
		{
			StubHttpMessageHandler handler = new (
				_ => Responses.Create( HttpStatusCode.InsufficientStorage, "disk full", "text/plain", "Custom Insufficient Storage" ) );
			Pug.HttpApiClient.HttpApiClient client = CreateClient( handler );

			Exception exception = await Record.ExceptionAsync( () => client.GetAsync( "widgets", null, null, null ) );

			Pug.HttpApiClient.InternalServerErrorException internalServerErrorException =
				Assert.IsType<Pug.HttpApiClient.InternalServerErrorException>( exception );
			Assert.Equal( HttpStatusCode.InsufficientStorage, internalServerErrorException.ResponseStatusCode );
			Assert.Equal( "Custom Insufficient Storage", internalServerErrorException.ResponseStatusReason );
			Assert.Equal( "disk full", internalServerErrorException.ResponseMessage );
		}

		[Fact]
		public async Task Get_429TooManyRequests_UnmappedStatusIsReturnedNotThrown()
		{
			StubHttpMessageHandler handler = new ( _ => Responses.Status( HttpStatusCode.TooManyRequests, "Too Many Requests" ) );
			Pug.HttpApiClient.HttpApiClient client = CreateClient( handler );

			HttpResponseMessage response = await client.GetAsync( "widgets", null, null, null );

			Assert.Equal( HttpStatusCode.TooManyRequests, response.StatusCode );
		}
	}
}
