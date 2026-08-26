using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using Xunit;

#pragma warning disable SYSLIB0050, SYSLIB0051 // legacy serialization surface is what is under test here

namespace Pug.HttpApiClient.Tests.Common
{
	/// <summary>
	/// Guards the SerializationInfo round trip of <see cref="HttpApiRequestException"/>.
	/// </summary>
	/// <remarks>
	/// The deserialization constructor used to read ResponseStatusCode from the ResponseMessage field name.
	/// GetObjectData always wrote all three fields correctly, so the defect only appeared on the way back in:
	/// the response BODY was fed to Convert.ToInt32, throwing FormatException for any non-numeric body. It
	/// went unnoticed because nothing exercised this constructor.
	/// </remarks>
	public class ExceptionSerializationTests
	{
		private static HttpResponseMessage Response( HttpStatusCode statusCode, string body, string reasonPhrase ) =>
			new ( statusCode )
			{
				Content = new StringContent( body ),
				ReasonPhrase = reasonPhrase
			};

		private static T RoundTrip<T>( T exception ) where T : Exception
		{
			SerializationInfo info = new ( typeof(T), new FormatterConverter() );
			StreamingContext context = new ( StreamingContextStates.All );

			exception.GetObjectData( info, context );

			return (T)Activator.CreateInstance(
					typeof(T),
					BindingFlags.NonPublic | BindingFlags.Instance,
					binder: null,
					args: new object[] { info, context },
					culture: null );
		}

		[Fact]
		public void HttpApiRequestException_RoundTripsAllThreeResponseFields()
		{
			// A deliberately NON-numeric body: under the old defect this is what reached Convert.ToInt32,
			// so this test fails with FormatException rather than a wrong value if the bug returns.
			HttpApiRequestException original =
				new ( "boom", Response( HttpStatusCode.Conflict, "resource already exists", "Conflict" ) );

			HttpApiRequestException revived = RoundTrip( original );

			Assert.Equal( HttpStatusCode.Conflict, revived.ResponseStatusCode );
			Assert.Equal( "Conflict", revived.ResponseStatusReason );
			Assert.Equal( "resource already exists", revived.ResponseMessage );
		}

		[Fact]
		public void HttpApiRequestException_RoundTripsStatusCodeIndependentlyOfBody()
		{
			// Body is numeric but different from the status code, so reading the wrong field would produce
			// a plausible-looking wrong value rather than an exception. Pins that the two are not confused.
			HttpApiRequestException original =
				new ( "boom", Response( HttpStatusCode.NotFound, "500", "Not Found" ) );

			HttpApiRequestException revived = RoundTrip( original );

			Assert.Equal( HttpStatusCode.NotFound, revived.ResponseStatusCode );
			Assert.Equal( "500", revived.ResponseMessage );
		}

		[Fact]
		public void UnknownResourceException_RoundTripsThroughItsBaseImplementation()
		{
			UnknownResourceException original =
				new ( Response( HttpStatusCode.Gone, "long gone", "Gone" ) );

			UnknownResourceException revived = RoundTrip( original );

			Assert.Equal( HttpStatusCode.Gone, revived.ResponseStatusCode );
			Assert.Equal( "long gone", revived.ResponseMessage );
		}

		[Fact]
		public void InternalServerErrorException_RoundTripsThroughItsBaseImplementation()
		{
			InternalServerErrorException original =
				new ( Response( HttpStatusCode.InternalServerError, "stack trace here", "Internal Server Error" ) );

			InternalServerErrorException revived = RoundTrip( original );

			Assert.Equal( HttpStatusCode.InternalServerError, revived.ResponseStatusCode );
			Assert.Equal( "stack trace here", revived.ResponseMessage );
		}
	}
}
