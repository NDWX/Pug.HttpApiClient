using System;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading.Tasks;
using Pug.HttpApiClient.Tests.Infrastructure;
using Xunit;

namespace Pug.HttpApiClient.Tests.NetStandard
{
	/// <summary>
	/// <see cref="HttpApiClient.EnsureSuccessResponse"/> maps unsuccessful statuses to exceptions through a
	/// switch statement that wraps the <c>HttpStatusCode.Locked</c> (423) and
	/// <c>HttpStatusCode.InsufficientStorage</c> (507) cases in <c>#if !NETSTANDARD</c>. On this TFM those two
	/// cases are compiled out entirely, so 423/507 responses fall through the switch unmatched and are RETURNED
	/// to the caller instead of being thrown - the exact inverse of the modern (net10.0) build.
	/// </summary>
	[Trait( "Category", "NetStandard" )]
	public class LegacyStatusMappingTests
	{
		private static HttpApiClient CreateClient( StubHttpMessageHandler handler ) =>
			new ( new Uri( "https://api.example.com/v1" ), new StubHttpClientFactory( handler ) );

		[Fact]
		public async Task GetAsync_Returns423Locked_InsteadOfThrowing_BecauseTheCaseIsCompiledOutOnNetStandard()
		{
			StubHttpMessageHandler handler = StubHttpMessageHandler.AlwaysReturns( HttpStatusCode.Locked );
			HttpApiClient client = CreateClient( handler );

			HttpResponseMessage response = await client.GetAsync( "resource/1", null, null, null );

			Assert.Equal( HttpStatusCode.Locked, response.StatusCode );
		}

		[Fact]
		public async Task GetAsync_Returns507InsufficientStorage_InsteadOfThrowing_BecauseTheCaseIsCompiledOutOnNetStandard()
		{
			StubHttpMessageHandler handler = StubHttpMessageHandler.AlwaysReturns( HttpStatusCode.InsufficientStorage );
			HttpApiClient client = CreateClient( handler );

			HttpResponseMessage response = await client.GetAsync( "resource/1", null, null, null );

			Assert.Equal( HttpStatusCode.InsufficientStorage, response.StatusCode );
		}

		[Theory]
		[InlineData( HttpStatusCode.Forbidden, typeof(Pug.AuthorizationException) )]
		[InlineData( HttpStatusCode.Unauthorized, typeof(AuthenticationException) )]
		[InlineData( HttpStatusCode.NotFound, typeof(UnknownResourceException) )]
		[InlineData( HttpStatusCode.Gone, typeof(UnknownResourceException) )]
		[InlineData( HttpStatusCode.BadRequest, typeof(HttpApiRequestException) )]
		[InlineData( HttpStatusCode.InternalServerError, typeof(InternalServerErrorException) )]
		public async Task GetAsync_StillMapsStatus_ToExpectedException_OnNetStandard( HttpStatusCode statusCode, Type expectedExceptionType )
		{
			StubHttpMessageHandler handler = StubHttpMessageHandler.AlwaysReturns( statusCode );
			HttpApiClient client = CreateClient( handler );

			await Assert.ThrowsAsync( expectedExceptionType, () => client.GetAsync( "resource/1", null, null, null ) );
		}

		[Fact]
		public async Task PatchAsync_SendsVerbBuiltAsNewHttpMethodPatch_AndTransportSeesPATCH()
		{
			// #if NETSTANDARD builds the verb as `new HttpMethod("PATCH")` rather than the HttpMethod.Patch
			// static used elsewhere - a different code path with the same observable wire method.
			StubHttpMessageHandler handler = StubHttpMessageHandler.AlwaysReturns( HttpStatusCode.OK );
			HttpApiClient client = CreateClient( handler );

			await client.PatchAsync( "resource/1", new StringContent( "{}" ), null, null, null );

			Assert.Equal( "PATCH", handler.LastRequest.Method.Method );
		}
	}
}
