using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Pug.HttpApiClient
{
	public class AuthorizationHeaderDecorator : IHttpRequestMessageDecorator
	{
		private readonly string _type;
		private readonly string _value;

		public AuthorizationHeaderDecorator( string type, string value )
		{
			if( string.IsNullOrWhiteSpace( type ) ) throw new ArgumentException( "Value cannot be null or whitespace.", nameof(type) );
			if( string.IsNullOrWhiteSpace( value ) ) throw new ArgumentException( "Value cannot be null or whitespace.", nameof(value) );
			_type = type;
			_value = value;
		}
		
		public virtual void Decorate( MessageDecorationContext context )
		{
			context.RequestHeaders.Authorization =
				new AuthenticationHeaderValue( _type, _value );
		}

		public virtual Task DecorateAsync( MessageDecorationContext context )
		{
			Decorate( context );

			return Task.CompletedTask;
		}
	}
}