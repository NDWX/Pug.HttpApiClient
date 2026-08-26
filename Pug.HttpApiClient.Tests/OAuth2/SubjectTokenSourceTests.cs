using System;
using Microsoft.AspNetCore.Http;
using Moq;
using Pug.HttpApiClient.OAuth2;
using Xunit;

namespace Pug.HttpApiClient.Tests.OAuth2
{
	public class SubjectTokenSourceTests
	{
		[Fact]
		public void StaticTokenExchangeSubjectTokenSource_GetSubjectToken_ReturnsConfiguredToken()
		{
			StaticTokenExchangeSubjectTokenSource source = new ( "configured-token" );

			Assert.Equal( "configured-token", source.GetSubjectToken() );
		}

		[Theory]
		[InlineData( null )]
		[InlineData( "" )]
		[InlineData( "   " )]
		public void StaticTokenExchangeSubjectTokenSource_Constructor_BlankOrNullToken_ThrowsArgumentException( string subjectToken )
		{
			ArgumentException exception = Assert.Throws<ArgumentException>( () => new StaticTokenExchangeSubjectTokenSource( subjectToken ) );

			Assert.Equal( "subjectToken", exception.ParamName );
		}

		private static HttpTokenExchangeSubjectTokenSource CreateSource( string authorizationHeader )
		{
			DefaultHttpContext httpContext = new ();

			if( authorizationHeader is not null )
				httpContext.Request.Headers["Authorization"] = authorizationHeader;

			Mock<IHttpContextAccessor> httpContextAccessor = new ();
			httpContextAccessor.Setup( x => x.HttpContext ).Returns( httpContext );

			return new HttpTokenExchangeSubjectTokenSource( httpContextAccessor.Object );
		}

		[Fact]
		public void HttpTokenExchangeSubjectTokenSource_MissingAuthorizationHeader_ReturnsNull()
		{
			HttpTokenExchangeSubjectTokenSource source = CreateSource( null );

			Assert.Null( source.GetSubjectToken() );
		}

		[Fact]
		public void HttpTokenExchangeSubjectTokenSource_NonBearerScheme_ReturnsNull()
		{
			HttpTokenExchangeSubjectTokenSource source = CreateSource( "Basic xyz" );

			Assert.Null( source.GetSubjectToken() );
		}

		[Fact]
		public void HttpTokenExchangeSubjectTokenSource_LowercaseBearerScheme_IsAccepted()
		{
			// The scheme check uses InvariantCultureIgnoreCase, so "bearer" (any casing) is accepted
			// even though the constant compared against is "BEARER ".
			HttpTokenExchangeSubjectTokenSource source = CreateSource( "bearer xyz123" );

			Assert.Equal( "xyz123", source.GetSubjectToken() );
		}
		[Theory]
		[InlineData( "BEARER abc123", "abc123" )]
		[InlineData( "Bearer abc123", "abc123" )]
		[InlineData( "bearer token-with-dashes", "token-with-dashes" )]
		[InlineData( "BEARER a", "a" )]
		[InlineData( "Bearer eyJhbGciOiJSUzI1NiJ9.e30.sig", "eyJhbGciOiJSUzI1NiJ9.e30.sig" )]
		public void HttpTokenExchangeSubjectTokenSource_ReturnsTokenWithoutSchemePrefix( string header, string expected )
		{
			// Regression: extraction used Substring( separatorIndex, length - ( separatorIndex + 1 ) ),
			// which for "BEARER abc123" produced " abc12" - the leading space kept and the final
			// character dropped. The single-character and JWT cases pin both ends of the slice.
			HttpTokenExchangeSubjectTokenSource source = CreateSource( header );

			Assert.Equal( expected, source.GetSubjectToken() );
		}
	}
}